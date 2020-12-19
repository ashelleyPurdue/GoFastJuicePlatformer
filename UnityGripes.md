I like to complain about things.  Here, I'm documenting my complaints about
Unity as I run into them on this project, in case I ever decide to submit it
as feedback.

And, of course, venting is thereputic.

# CharacterController
* Maintains its own internal `position` field which trumps `transform.position`,
    and does not expose any kind of `.SetPosition()` method.  This means
    teleportation---something that should be DEAD SIMPLE---requires a workaround.
    