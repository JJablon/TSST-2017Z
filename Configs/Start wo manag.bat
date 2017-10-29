cd WireCloud
start cmd /c cloudStart.bat
cd ..
cd NetworkClient 
start cmd /c startClient0-A.bat
start cmd /c startClient1-B.bat
start cmd /c startClient2-C.bat
start cmd /c startClient3-D.bat
cd ..
cd NetworkNode
start cmd /c startNode0.bat
start cmd /c startNode1.bat
start cmd /c startNode2.bat
start cmd /c startNode3.bat
exit
