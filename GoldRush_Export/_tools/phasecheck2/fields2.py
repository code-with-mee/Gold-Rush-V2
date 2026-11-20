import sys, os
sys.path.insert(0, '.')
from fields import serialized, vers, real
for rel in ["UI/GoldRushMenu.cs","Gameplay/PlayerMotor.cs","Gameplay/ThirdPersonCamera.cs","World/ExitGate.cs","World/Coin.cs","World/PowerUp.cs","Gameplay/GameManager.cs","Core/Data.cs","UI/Bob.cs","UI/VirtualJoystick.cs","UI/TouchLookArea.cs"]:
    print("==", rel, "REAL:", serialized(real[rel]))
    for n, c, src in vers[rel]:
        f = serialized(c)
        print("   p%d" % n, "same-as-real" if f == serialized(real[rel]) else f)
