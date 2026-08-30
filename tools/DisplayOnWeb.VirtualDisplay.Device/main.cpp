#include <windows.h>
#include <swdevice.h>
#include <sddl.h>
#include <string>
#include <iostream>
#include <cstring>

constexpr wchar_t ServiceName[] = L"LANternDeviceService";
constexpr wchar_t PipeName[] = L"\\\\.\\pipe\\LANtern.DeviceService";

SERVICE_STATUS_HANDLE g_statusHandle = nullptr;
SERVICE_STATUS g_status{};
HANDLE g_stopEvent = nullptr;
HSWDEVICE g_device = nullptr;

VOID WINAPI CreationCallback(HSWDEVICE, HRESULT, PVOID context, PCWSTR)
{
    SetEvent(*static_cast<HANDLE*>(context));
}

bool ConnectDisplay()
{
    if (g_device) return true;
    HANDLE created = CreateEvent(nullptr, FALSE, FALSE, nullptr);
    if (!created) return false;
    SW_DEVICE_CREATE_INFO info{};
    info.cbSize = sizeof(info);
    info.pszzCompatibleIds = L"DisplayOnWebVirtualDisplay\0\0";
    info.pszInstanceId = L"DisplayOnWebVirtualDisplay";
    info.pszzHardwareIds = L"DisplayOnWebVirtualDisplay\0\0";
    info.pszDeviceDescription = L"LANtern Virtual Monitor";
    info.CapabilityFlags = SWDeviceCapabilitiesRemovable | SWDeviceCapabilitiesSilentInstall | SWDeviceCapabilitiesDriverRequired;
    HRESULT hr = SwDeviceCreate(L"DisplayOnWebVirtualDisplay", L"HTREE\\ROOT\\0", &info, 0, nullptr, CreationCallback, &created, &g_device);
    bool ok = SUCCEEDED(hr) && WaitForSingleObject(created, 10000) == WAIT_OBJECT_0;
    CloseHandle(created);
    if (!ok && g_device) { SwDeviceClose(g_device); g_device = nullptr; }
    return ok;
}

void DisconnectDisplay()
{
    if (g_device) { SwDeviceClose(g_device); g_device = nullptr; }
}

void ReportServiceStatus(DWORD state, DWORD error = NO_ERROR)
{
    g_status.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
    g_status.dwCurrentState = state;
    g_status.dwWin32ExitCode = error;
    g_status.dwControlsAccepted = state == SERVICE_RUNNING ? SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_SHUTDOWN : 0;
    SetServiceStatus(g_statusHandle, &g_status);
}

DWORD WINAPI ServiceControl(DWORD control, DWORD, LPVOID, LPVOID)
{
    if (control == SERVICE_CONTROL_STOP || control == SERVICE_CONTROL_SHUTDOWN)
    {
        ReportServiceStatus(SERVICE_STOP_PENDING);
        SetEvent(g_stopEvent);
    }
    return NO_ERROR;
}

HANDLE CreateControlPipe()
{
    PSECURITY_DESCRIPTOR descriptor = nullptr;
    ConvertStringSecurityDescriptorToSecurityDescriptor(
        L"D:(A;;GA;;;SY)(A;;GA;;;BA)(A;;GRGW;;;IU)", SDDL_REVISION_1, &descriptor, nullptr);
    SECURITY_ATTRIBUTES attributes{ sizeof(SECURITY_ATTRIBUTES), descriptor, FALSE };
    HANDLE pipe = CreateNamedPipe(PipeName, PIPE_ACCESS_DUPLEX, PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_NOWAIT,
        1, 256, 256, 0, descriptor ? &attributes : nullptr);
    if (descriptor) LocalFree(descriptor);
    return pipe;
}

void RunPipeServer()
{
    HANDLE pipe = CreateControlPipe();
    if (pipe == INVALID_HANDLE_VALUE) return;
    while (WaitForSingleObject(g_stopEvent, 50) == WAIT_TIMEOUT)
    {
        BOOL connected = ConnectNamedPipe(pipe, nullptr);
        DWORD error = connected ? ERROR_SUCCESS : GetLastError();
        if (!connected && error != ERROR_PIPE_CONNECTED)
        {
            if (error == ERROR_PIPE_LISTENING || error == ERROR_NO_DATA) continue;
            break;
        }

        char buffer[64]{};
        DWORD read = 0;
        BOOL received = FALSE;
        for (int attempt = 0; attempt < 100 && WaitForSingleObject(g_stopEvent, 10) == WAIT_TIMEOUT; attempt++)
        {
            received = ReadFile(pipe, buffer, sizeof(buffer) - 1, &read, nullptr);
            if (received && read) break;
            if (GetLastError() != ERROR_NO_DATA) break;
        }
        if (received && read)
        {
            std::string command(buffer, read);
            const char* response = "ERROR";
            if (command.find("CONNECT") == 0) response = ConnectDisplay() ? "OK CONNECTED" : "ERROR CONNECT";
            else if (command.find("DISCONNECT") == 0) { DisconnectDisplay(); response = "OK DISCONNECTED"; }
            else if (command.find("STATUS") == 0) response = g_device ? "OK CONNECTED" : "OK DISCONNECTED";
            DWORD written = 0;
            WriteFile(pipe, response, static_cast<DWORD>(strlen(response)), &written, nullptr);
        }
        FlushFileBuffers(pipe);
        DisconnectNamedPipe(pipe);
    }
    CloseHandle(pipe);
}

VOID WINAPI ServiceMain(DWORD, LPWSTR*)
{
    g_statusHandle = RegisterServiceCtrlHandlerEx(ServiceName, ServiceControl, nullptr);
    if (!g_statusHandle) return;
    ReportServiceStatus(SERVICE_START_PENDING);
    g_stopEvent = CreateEvent(nullptr, TRUE, FALSE, nullptr);
    if (!g_stopEvent) { ReportServiceStatus(SERVICE_STOPPED, GetLastError()); return; }
    ReportServiceStatus(SERVICE_RUNNING);
    RunPipeServer();
    DisconnectDisplay();
    CloseHandle(g_stopEvent);
    ReportServiceStatus(SERVICE_STOPPED);
}

int RunConsole()
{
    std::wcout << L"LANtern virtual display connected. Press Enter to disconnect.\n";
    if (!ConnectDisplay()) return 1;
    std::wstring line;
    std::getline(std::wcin, line);
    DisconnectDisplay();
    return 0;
}

int wmain(int argc, wchar_t* argv[])
{
    if (argc > 1 && _wcsicmp(argv[1], L"--service") == 0)
    {
        SERVICE_TABLE_ENTRY table[] = { { const_cast<LPWSTR>(ServiceName), ServiceMain }, { nullptr, nullptr } };
        return StartServiceCtrlDispatcher(table) ? 0 : static_cast<int>(GetLastError());
    }
    return RunConsole();
}
