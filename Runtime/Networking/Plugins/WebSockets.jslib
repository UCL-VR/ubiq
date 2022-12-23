mergeInto(LibraryManager.library, {

    ws: undefined,

    JsWebSocketPlugin_TryConnect: function(url) {
        if (Module.myws !== undefined) {
            return false;
        }

        Module.myws = new WebSocket(new URL(UTF8ToString(url)));
        Module.myws.binaryType = 'arraybuffer';
        Module.myws.messages = [];
        Module.myws.onmessage = function(ev) {
            Module.myws.messages.push(new Uint8Array(ev.data));
           
        }
        return true;
    },

    JsWebSocketPlugin_IsConnecting: function() {
        return Module.myws !== undefined && Module.myws.readyState === 0;
    },

    JsWebSocketPlugin_IsOpen: function() {
        return Module.myws !== undefined && Module.myws.readyState === 1;
    },

    JsWebSocketPlugin_IsClosing: function() {
        return Module.myws !== undefined && Module.myws.readyState === 2;
    },

    JsWebSocketPlugin_IsClosed: function() {
        return Module.myws !== undefined && Module.myws.readyState === 3;
    },

    JsWebSocketPlugin_Send: function(bytes, start, length) {
        if (Module.myws === undefined || Module.myws.readyState >= 2) {
            // Closing or closed
            return -1;
        } else if (Module.myws.readyState == 0)
        {
            // Connecting
            return 0;
        }

        Module.myws.send(new Uint8Array(Module.HEAPU8.buffer,bytes+start,length));
        return 1;
    },

    JsWebSocketPlugin_Receive: function(bytes, offset, length) {
        if (Module.myws.messages.length > 0) {
            var m = Module.myws.messages[0];
            if (length > m.length) {
                length = m.length;
            }

            var outArr = new Uint8Array(Module.HEAPU8.buffer,bytes+offset,length);

            if (length < m.length) {
                outArr.set(Module.myws.messages[0].slice(0,length));
                Module.myws.messages[0] = Module.myws.messages[0].slice(length);
            } else {
                outArr.set(Module.myws.messages[0]);
                Module.myws.messages.splice(0,1);
            }

            return length;
        }

        if (Module.myws === undefined || Module.myws.readyState >= 2) {
            // Closing or closed
            return -1;
        }

        return 0;
    },

    JsWebSocketPlugin_Close: function() {
        Module.myws.close();
    }
});

