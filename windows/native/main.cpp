#include <iostream>
#include <string>
#include <thread>
#include <vector>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#endif

void handle_client(SOCKET client_socket) {
    char buffer[4096];
    std::cout << "[RTSP] Client connected.\n";
    
    while (true) {
        int bytes_read = recv(client_socket, buffer, sizeof(buffer) - 1, 0);
        if (bytes_read > 0) {
            buffer[bytes_read] = '\0';
            std::cout << "\n--- Received Request ---\n" << buffer << "------------------------\n";
            
            // Send a dummy 200 OK response
            std::string response = 
                "RTSP/1.0 200 OK\r\n"
                "CSeq: 1\r\n"
                "Server: OpenReceiver/0.1\r\n"
                "\r\n";
            
            send(client_socket, response.c_str(), response.length(), 0);
        } else if (bytes_read == 0) {
            std::cout << "[RTSP] Client disconnected.\n";
            break;
        } else {
            std::cerr << "[RTSP] Recv failed.\n";
            break;
        }
    }
    closesocket(client_socket);
}

int main() {
    std::cout << "Starting OpenReceiver Windows PoC...\n";

#ifdef _WIN32
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cerr << "WSAStartup failed.\n";
        return 1;
    }
#endif

    SOCKET server_socket = socket(AF_INET, SOCK_STREAM, 0);
    if (server_socket == INVALID_SOCKET) {
        std::cerr << "Socket creation failed.\n";
        return 1;
    }

    sockaddr_in server_addr;
    server_addr.sin_family = AF_INET;
    server_addr.sin_addr.s_addr = INADDR_ANY;
    server_addr.sin_port = htons(7000); // Standard AirPlay RTSP port

    if (bind(server_socket, (struct sockaddr*)&server_addr, sizeof(server_addr)) == SOCKET_ERROR) {
        std::cerr << "Bind failed. Port 7000 might be in use.\n";
        return 1;
    }

    if (listen(server_socket, SOMAXCONN) == SOCKET_ERROR) {
        std::cerr << "Listen failed.\n";
        return 1;
    }

    std::cout << "Listening for RTSP connections on port 7000...\n";

    while (true) {
        SOCKET client_socket = accept(server_socket, nullptr, nullptr);
        if (client_socket != INVALID_SOCKET) {
            std::thread(handle_client, client_socket).detach();
        }
    }

#ifdef _WIN32
    WSACleanup();
#endif
    return 0;
}
