# Heartbeat

RoomClient instances will ping the RoomServer at a 1 Hz. If they do not receive a response after 5 seconds, they consider the server to have timed out, and will notify the user.

