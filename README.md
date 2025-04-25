https://github.com/user-attachments/assets/b38012d1-7d58-4256-89c7-9aa81951c4f1

This project is a quick prototype of a multiplayer racing game using Photon Fusion 2.  
Players can enter a nickname, join a lobby, start a race, drive cars, and see a finish screen with results.  
Everything works across scenes, and core systems are modular.

What could be improved:
Right now, many objects like the track and views are already in the scenes. In production, they should be instantiated at runtime.  
There’s no real game loop (no Restart, Pause).  
I’m not handling errors well, like connection failures.  
The views and gameplay are pretty stub.

Overall, a solid prototype. With more time, I’d focus on polish, better object management, and adding more game flow features.
