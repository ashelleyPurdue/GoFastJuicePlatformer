using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerConstants
{
    public const float BODY_HEIGHT = 1.4558f;
    public const float BODY_RADIUS = 0.375f;
    
    public const float SHORT_JUMP_DECAY_RATE = 0.7f;

    // These constants will determine the initial jump velocity
    // and rising/falling gravity strength
    public const float FIRST_JUMP_HEIGHT = 5;
    public const float FIRST_JUMP_RISE_TIME = 0.404f;
    public const float FIRST_JUMP_FALL_TIME = 0.328f;

    // This constant will determine the initial jump velocity when doing
    // a chained jump.
    public const float SECOND_JUMP_HEIGHT = 8;

    public const float HSPEED_MIN = 2;
    public const float HSPEED_MAX_GROUND = 8;
    public const float HSPEED_MAX_AIR = 20;

    public const float TERMINAL_VELOCITY_AIR = -100;
    public const float TERMINAL_VELOCITY_WALL_SLIDE = -10;

    public const float HACCEL_GROUND = 15;
    public const float HACCEL_AIR = 30;
    public const float HACCEL_AIR_BACKWARDS = 30;
    public const float MIN_LANDING_HSPEED_MULT = 0.25f;

    public const float BONK_SPEED = -3;
    public const float LEDGE_GRAB_VSPEED = 11;
    public const float LEDGE_GRAB_HSPEED = 4;
    public const float LEDGE_GRAB_DURATION = 0.15f;

    public const float DIVE_JUMP_HEIGHT = 4;
    public const float DIVE_GRAVITY = 100;
    public const float DIVE_HSPEED_INITIAL = 20;
    public const float DIVE_HSPEED_FINAL_MAX = 10;
    public const float DIVE_HSPEED_FINAL_MIN = 5f;
    public const float DIVE_HSPEED_SLOW_TIME = 0.5f;

    public const float ROT_SPEED_DEG = 360 * 2;
    public const float FRICTION_GROUND = 15;
    public const float FRICTION_WALL_SLIDE = 10;

    public const float COYOTE_TIME = 0.1f;      // Allows you to press the jump button a little "late" and still jump
    public const float EARLY_JUMP_TIME = 0.1f;  // Allows you to press the jump button a little "early" and still jump
    
    // If you jump again shortly after you land, you'll do a "chained jump."
    // This is like the "double jump" from 3D Mario games.
    public const float CHAINED_JUMP_HSPEED_MULT = 1.3f;
    public const float CHAINED_JUMP_TIME_WINDOW = 0.1f;

    public const float WALL_JUMP_HSPEED_MULT = 1.1f;

    public const float MAX_PIVOT_SPEED = 0.25f; // If you're below this speed, you can pivot on a dime.

    public const float JUMP_REDIRECT_TIME = 0.1f;
    
    public const float ROLL_TIME = 0.25f;
    public const float ROLL_REDIRECT_TIME = ROLL_TIME / 3;
    public const float ROLL_COOLDOWN = 0.5f;
    public const float ROLL_DISTANCE = 5;
    public const float ROLL_JUMP_HSPEED = 10;

    public const float LEFT_STICK_DEADZONE = 0.001f;
}
