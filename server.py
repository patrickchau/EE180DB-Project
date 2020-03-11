#!/usr/local/bin/python3.7
# each client must have an IP, MAC, row, col associated
class client:
    def __init__(self):
        self.macId=None
        self.ipAddr = None
        self.row=None
        self.col=None
        self.buttons=""
        self.player=""

from collections import defaultdict
import socket
from _thread import *
import threading
import time
import errno

clientList = []
controllerList = []
macToClient=defaultdict(client) # map for mac->client
macToId=defaultdict(int)        # mac to client ID
print_lock=threading.Lock()     # print locks
addLock=threading.Lock()        # add client locks

#Hardware token
hw_token=client()
hw_token_socket=None
curr_row = 0
curr_col= 0
unity_c = None


# linked list variables - maybe move to own object?
size=0
id=0
dummyHead=client()
dummyHead.next=dummyHead
dummyHead.prev=dummyHead
tail=dummyHead
startPointer=dummyHead

lightUp = []   # list of MACs for clients to light up
interval=1

def each_client(c):
    global macToClient
    global macToId
    global size
    global id
    global dummyHead
    global tail
    global lightUp
    global curr_row
    global curr_col
    global hw_token_socket
    global hw_token
    global controllerList

    count = 1
    macId=None
    while True:
        
        buffer=None
        c.settimeout(10000) #timeout 10 secs for now,client need to constantly send msg
        try:
            buffer=c.recv(4096).decode('utf-8')
            #print("received message: " + buffer)
        except IOError or socket.timeout:  # timeout error catch
            print_lock.acquire()
            print("time out occured on ",c)
            print_lock.release()
            # if timeout, then close the connection to client
            if macId:
                target=macToClient[macId]
                addLock.acquire()
                target.prev.next=target.next
                target.next.prev=target.prev
                del macToClient[macId]
                size-=1

                lightUp.remove(macId)
                addLock.release()
                
            return #we are out of here
        except socket.error as e:   # catch if connection is reset (i.e client code is stopped)
            print_lock.acquire()
            print("in this section",c)
            print_lock.release()
            if e.errno != errno.ECONNRESET:
                raise
            lightUp.remove(macId)
            break
            

        # Block to parse all possible client inputs
        try:
            _type,msg=buffer.split(",") # assume client sends in this format
        except ValueError:
            _type="error"
            msg=""
            if macId in lightUp:
                lightUp.remove(macId)
        #print("type: " + _type)
        if _type=="start":
            print("this is a start message!")
            if not hw_token.macId:
                data="hw token not ready yet!"
                c.send(data.encode('utf-8'))
                continue

            # print locks
            print_lock.acquire()
            print("add a new client ",c)
            print_lock.release()

            # create new client to macID -> client map
            macId=msg
            newClient=client()
            newClient.macId=msg
            if macToClient[msg].macId == msg:
                data="client already exists"
                lightUp.append(macId)
                c.send(data.encode('utf-8'))
                continue

            # update the linked list for snake
            addLock.acquire()
            newClient.prev=tail
            tail.next=newClient
            newClient.next=dummyHead
            dummyHead.prev=newClient
            tail=newClient
            macToClient[macId]=newClient
            size+=1
            id+=1
            macToId[macId]=id
            macToClient[macId].row = curr_row
            macToClient[macId].col = curr_col
            data = "success"
            hw_token_socket.send(data.encode('utf-8'))
            if not macId in lightUp:
                lightUp.append(macId) # this adds new client to light up
            addLock.release()

            # let client know that it is now registered
            data="registered"
            c.send(data.encode('utf-8'))
        elif _type == "hwstart":
            macId=msg
            print("Controller connnection!")
            if not macId in controllerList:
                controllerList.append(macId)
                macToClient[macId].player = "P" + str(len(controllerList)) + ":";
                print(macToClient[macId].player)
            hw_token.macId=msg
            hw_token_socket=c
            
            # let token know that it is now registered
            data="registered"
            c.send(data.encode('utf-8'))
        elif _type == "pushed":
            macToClient[macId].buttons = macToClient[macId].player + msg[0:11]
        elif _type=="close":
            # if client sends close request separate from socket timeout
            print("this is a closing message")
            print("mac id: " + macId)
            target=macToClient[macId]
            addLock.acquire()

            # change this to a (try, except) block for when target is Null
            if not target.next:
                target.prev.next=target.next
            if not target.prev:
                target.next.prev=target.prev

            size-=1
            lightUp.remove(macId)
            addLock.release()
            data="closed"
            c.send(data.encode('utf-8'))
            break
        elif _type=="position": # position message of format "position, row.col"
            #print("now getting row and column from hardware token")
            row,col = msg.split('.')
            curr_row = row
            curr_col = col

        # block to send data to client
        if macId in lightUp:
            print("client needs to be lit")
            data="lightUp " + macId + " " + str(count)
            count = count + 1
            c.send(data.encode('utf-8'))
        else:
            data = "nothing"
            try:
                c.send(data.encode('utf-8'))
            except BrokenPipeError:
                break;

    # if we break from the while True, close the client connection.
    # this should only happen when we send a close statement
    c.close()

def lookForConnection(serv):
    while True:
        c,addr=serv.accept()
        print_lock.acquire()
        print("connection established\n")
        print_lock.release()
        start_new_thread(each_client,(c,))
    serv.close()
    
def loopThrough():

    while True:
        buttons_pushed = ""
        for c in controllerList:
            buttons_pushed += macToClient[c].buttons
        print(buttons_pushed)
        try:
            unity_c.send((buttons_pushed + "\n").encode())
        except:
            socket.close(unity_c)

            #reeattempt communication
            global unity_c
            unity_c = socket.socket(socket.AF_INET, socket.SOCK_STREAM)        
            hostname=socket.gethostname()
            ipAddr=socket.gethostbyname(hostname)
            unity_c.connect((ipAddr, 7575))
            unity_c.send("".encode())
        time.sleep(0.1)

def runServer():

    global unity_c
    unity_c = socket.socket(socket.AF_INET, socket.SOCK_STREAM)        
    hostname=socket.gethostname()
    ipAddr=socket.gethostbyname(hostname)
    unity_c.connect((ipAddr, 7575))
    unity_c.send("".encode())

    host="192.168.43.190"
    serv=socket.socket(socket.AF_INET,socket.SOCK_STREAM)
    serv.bind((host,8080))
    serv.listen(10) #in case it takes time to acquire lock 



    t1 = threading.Thread(target=lookForConnection, args=(serv,)) 
    t2 = threading.Thread(target=loopThrough, ) 
  
    t1.start() 
    t2.start()
    
if __name__ == "__main__":
    runServer()


