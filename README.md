# vikinet2
A C# library to simplify TCP and UDP network traffic, for games or whatever else you may need.
I probably wont focus on updating this repo too much, considering the purpose of this is just to avoid rewriting the entire networking stack again.

Supports both `UDP` and `TCP` transports. `UDP` as of now, is only implemented for unreliable data transfer.

The project is configured to be a DLL by default, however feel very, very free to just copy and paste all the files!

# Things that need to be added !! (At least from what I've noticed so far)
 - GetPlayer by identifier
 - GetPlayer functions on NetClient instead of only NetServer
 - GetPlayer by username
 - Detach the loop inside NetServer.Start, so that Start doesn't also mean "Run"
