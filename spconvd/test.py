import threading, random, json, socket, array, math

HOST = "127.0.0.1"
PORT = 11111

mysock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
mysock.connect((HOST, PORT))
while True:
    data = mysock.recv(32)
    arr = array.array('i', data)
    if arr[0] == 0:
        args = {'x': arr[1], 'y': arr[2], 'z': arr[3], 'rx': arr[4], 'ry': arr[5], 'rz': arr[6]}
        print(args)
