// src/characters/Powerups.cs (stub)
namespace WorldTree;
public static class Powerups {
    public class HealthBoost(Environment env, (int,int) pos) : Powerup { public override void Update(){} }
    public class DoubleJump(Environment env, (int,int) pos) : Powerup { public override void Update(){} }
    public class MoreSeeds(Environment env, (int,int) pos) : Powerup { public override void Update(){} }
    public class Spike(Environment env, (int,int) pos, int width) : Powerup { public override void Update(){} }
    public class Lava(Environment env, (int,int) pos, int width) : Powerup { public override void Update(){} }
}
