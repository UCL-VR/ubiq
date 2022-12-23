var unityWebRtcInteropLibrary = {
    $context: {
        instances: {},

        lastId: 0,

        audioContext: null,
        whenUserMedia: null,

        signallingMessageType: {
            sessionDescription: 0,
            iceCandidate: 1
        },

        iceConnectionState: {
            closed: 0,
            failed: 1,
            disconnected: 2,
            new: 3,
            checking: 4,
            connected: 5
        },

        peerConnectionState: {
            closed: 0,
            failed: 1,
            disconnected: 2,
            new: 3,
            connecting: 4,
            connected: 5
        },

        error: function (e) {
            if (typeof e == typeof {}) {
                alert("Oh no! " + JSON.stringify(e));
            } else {
                alert("Oh no! " + e);
            }
        }
    },

    JS_WebRTC_New: function() {
        let id = context.lastId++;
        context.instances[id] = {
            startArgs: {
                polite: false,
                iceServers: [],
            },
            pc: null,
            msgs: [],
            panner: null,
            samples: 0,
            volume: 0,
            stats: {
                samples: 0,
                volume: 0
            },
            sampleRate: 1,
            isReady: false
        };
        return id;
    },

    JS_WebRTC_New_AddIceCandidate: function(id,urls,username,password) {
        let instance = context.instances[id];
        if (!instance || !instance.startArgs) {
            // No construction begun, or construction complete already
            return;
        }

        if (!username || username[0] == 0 || !password || password[0] == 0) {
            instance.startArgs.iceServers.push({ urls:UTF8ToString(urls) });
        } else {
            instance.startArgs.iceServers.push({
                urls: UTF8ToString(urls),
                username: UTF8ToString(username),
                credential: UTF8ToString(password),
                credentialType: "password"
            });
        }
    },

    JS_WebRTC_New_SetPolite: function(id,polite) {
        let instance = context.instances[id];
        if (!instance || !instance.startArgs) {
            // No construction begun, or construction complete already
            return;
        }

        instance.startArgs.polite = polite;
    },

    // JS_WebRTC_Init: function() {
    //     navigator.getUserMedia =
    //         navigator.getUserMedia || navigator.webkitGetUserMedia ||
    //         navigator.mozGetUserMedia || navigator.msGetUserMedia;

    //     if (!navigator.getUserMedia){
    //         return false;
    //     }

    //     if (context.audioContext.state == "closed") {
    //         context.audioContext = new AudioContext();
    //     }

    //     if (context.audioContext.state == "suspended") {
    //         context.audioContext.resume();
    //     }

    //     if (context.audioContext.state == "suspended") {
    //         return false;
    //     }
    // }

    JS_WebRTC_New_Start: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.startArgs) {
            // No construction begun, or construction complete already
            return;
        }

        let polite = instance.startArgs.polite;
        let iceServers = instance.startArgs.iceServers;
        delete instance.startArgs;

        if (!context.audioContext) {
            context.audioContext = new AudioContext();
        }

        if (!context.whenUserMedia) {
            context.whenUserMedia = navigator.mediaDevices.getUserMedia({audio:true});
        }

        instance.panner = new PannerNode(context.audioContext, {
            panningModel: 'HRTF',
            distanceModel: 'inverse',
            positionX: 0,
            positionY: 0,
            positionZ: 0,
            orientationX: 1,
            orientationY: 0,
            orientationZ: 0,
            refDistance: 1,
            maxDistance: 10000,
            rolloffFactor: 1,
            // coneInnerAngle: innerCone,
            // coneOuterAngle: outerCone,
            // coneOuterGain: outerGain
        });
        instance.pc = new RTCPeerConnection({
            iceServers: iceServers
        });
        instance.pc.ontrack = function(event) {
            console.log(JSON.stringify(event.track.getSettings()));
            let stream = new MediaStream([event.track]);

            // Workaround for Chrome issue where WebRTC streams won't play
            // unless connected to a HTML5 audio element
            // https://stackoverflow.com/questions/24287054/
            // https://bugs.chromium.org/p/chromium/issues/detail?id=933677
            let a = new Audio();
            a.muted = true;
            a.srcObject = stream;
            a.addEventListener('canplaythrough', () => {
                a = null;
            });

            let audioContext = context.audioContext;
            instance.processorNode = audioContext.createScriptProcessor(4096, 1, 1);
            instance.processorNode.onaudioprocess = function(event) {
                let arr = event.inputBuffer.getChannelData(0);
                let length = arr.length;
                for (let i = 0; i < length; i++) {
                    instance.volume += Math.abs(arr[i]);
                }
                instance.samples += length;
                instance.sampleRate = event.inputBuffer.sampleRate;
            };
            instance.gainNode = new GainNode(audioContext,{gain:0});
            instance.src = audioContext.createMediaStreamSource(stream);
            instance.src.connect(instance.processorNode);
            instance.src.connect(instance.panner);
            instance.processorNode.connect(instance.gainNode);
            instance.gainNode.connect(audioContext.destination);
            instance.panner.connect(audioContext.destination);
            // instance.pc.getTransceivers().forEach(transceiver => {
            //  console.log("tr: " + transceiver.receiver ? JSON.stringify(transceiver.receiver.getParameters()) : "no recv");
            //  console.log("tr: " + transceiver.sender ? JSON.stringify(transceiver.sender.getParameters()) : "no send");
            // });



            // var audioContext = context.audioContext;
            // instance.processorNode = audioContext.createScriptProcessor(4096, 1, 1);
            // instance.processorNode.onaudioprocess = function(event) {
            //     var arr = event.inputBuffer.getChannelData(0);
            //     var length = arr.length;

            //     for (var i = 0; i < length; i++) {
            //         instance.volume += Math.abs(arr[i]);
            //     }

            //     instance.samples += length;
            //     instance.sampleRate = event.inputBuffer.sampleRate;
            // }

            // instance.src = audioContext.createMediaStreamSource(stream);
            // instance.src.connect(instance.processorNode);
            // instance.processorNode.connect(instance.panner);
            // // instance.src.connect(instance.panner);
            // instance.panner.connect(audioContext.destination);
            // // instance.gain = new GainNode(instance.audioContext,{gain:1});
            // // instance.src.connect(instance.gain);
            // // instance.gain.connect(instance.audioContext.destination);
            // // instance.src.connect(audioContext.destination);

            // instance.pc.getTransceivers().forEach(transceiver => {
            //     console.log("tr: " + transceiver.receiver ? JSON.stringify(transceiver.receiver.getParameters()) : "no recv");
            //     console.log("tr: " + transceiver.sender ? JSON.stringify(transceiver.sender.getParameters()) : "no send");
            // });
        };
        instance.pc.onicecandidate = function(event) {
            if (event.candidate) {
                instance.msgs.push({
                    type: context.signallingMessageType.iceCandidate,
                    args: JSON.stringify(event.candidate)
                });
            }
        };

        // navigator.getUserMedia =
        //     navigator.getUserMedia || navigator.webkitGetUserMedia ||
        //     navigator.mozGetUserMedia || navigator.msGetUserMedia;

        context.whenUserMedia
        .then((stream) => {
            let instance = context.instances[id];
            if (!instance) {
                // Close() called before getUserMedia returned
                return;
            }

            for (const track of stream.getTracks()) {
                instance.pc.addTrack(track);
            }

            if (!polite) {
                instance.pc.createOffer(function(offer) {
                    console.log("Created offer" + JSON.stringify(offer));
                    instance.pc.setLocalDescription(offer, function() {
                        console.log("setLocalDescription, sending to remote");
                        instance.msgs.push({
                            type: context.signallingMessageType.sessionDescription,
                            args: JSON.stringify(offer)
                        });
                    }, context.error);
                }, context.error);
            }

            instance.isReady = true;
        })
        .catch((error) => {
            let instance = context.instances[id];
            if (!instance) {
                // Close() called before getUserMedia returned
                return;
            }

            if (instance.pc) {
                instance.pc.close();
            }

            delete context.instances[id];

            console.error("Closing this Peer Connection. Reason: " + error);
        });
        // navigator.getUserMedia({audio:true}, function(stream) {
        //     let instance = context.instances[id];
        //     if (!instance) {
        //         // Close() called before getUserMedia returned
        //         return;
        //     }

        //     for (const track of stream.getTracks()) {
        //         instance.pc.addTrack(track);
        //     }

        //     if (!polite) {
        //         instance.pc.createOffer(function(offer) {
        //             console.log("Created offer" + JSON.stringify(offer));
        //             instance.pc.setLocalDescription(offer, function() {
        //                 console.log("setLocalDescription, sending to remote");
        //                 instance.msgs.push({
        //                     type: context.signallingMessageType.sessionDescription,
        //                     args: JSON.stringify(offer)
        //                 });
        //             }, context.error);
        //         }, context.error);
        //     }

            //
            // var audioContext = context.audioContext;
            // instance.debug = {};
            // instance.debug.src = audioContext.createMediaStreamSource(stream);
            // instance.debug.processorNode = audioContext.createScriptProcessor(4096, 1, 1);
            // instance.debug.processorNode.onaudioprocess = function(event) {
            //     var arr = event.inputBuffer.getChannelData(0);
            //     var length = arr.length;
            //     var volume = 0;

            //     for (var i = 0; i < length; i++) {
            //     volume += Math.abs(arr[i]);
            //     }

            //     console.log(volume/length);
            // };
            // instance.debug.gainNode = new GainNode(audioContext,{gain:0});

            // instance.debug.src.connect(instance.debug.processorNode);
            // instance.debug.processorNode.connect(instance.debug.gainNode);
            // instance.debug.gainNode.connect(audioContext.destination);
            // instance.processorNode.connect(instance.gainNode);
            // instance.gainNode.connect(audioContext.destination);
            //

        //     if (!polite) {
        //         instance.pc.createOffer(function(offer) {
        //             console.log("Created offer" + JSON.stringify(offer));
        //             instance.pc.setLocalDescription(offer, function() {
        //                 console.log("setLocalDescription, sending to remote");
        //                 instance.msgs.push({
        //                     type: context.signallingMessageType.sessionDescription,
        //                     args: JSON.stringify(offer)
        //                 });
        //             }, context.error);
        //         }, context.error);
        //     }
        //   }, context.error);
    },

    JS_WebRTC_ResumeAudioContext: function() {
        if (!context.audioContext){
            context.audioContext = new AudioContext();
        }

        if (context.audioContext.state == "suspended") {
            context.audioContext.resume();
        }

        return context.audioContext.state != "suspended";
    },

    JS_WebRTC_Close: function(id) {
        let instance = context.instances[id];
        if (!instance) {
            return;
        }

        if (instance.pc) {
            instance.pc.close();
        }

        if (instance.stream) {
            for (const track of instance.stream.getTracks()) {
                track.stop();
            }
        }

        delete context.instances[id];
    },

    JS_WebRTC_IsSetup: function(id) {
        let instance = context.instances[id];
        return Boolean(instance && instance.isReady);
    },

    JS_WebRTC_ProcessSignallingMessage: function(id,type,args) {
        let instance = context.instances[id];
        if (!instance || !instance.isReady) {
            return false;
        }

        switch(type) {
            case context.signallingMessageType.sessionDescription:
                let desc = JSON.parse(UTF8ToString(args));
                switch(desc.type) {
                    case "answer":
                        instance.pc.setRemoteDescription((desc), function() {
                            console.log("Call established!");
                        }, context.error);
                        break;
                    case "offer":
                        instance.pc.setRemoteDescription((desc), function() {
                            console.log("setRemoteDescription, creating answer");
                            instance.pc.createAnswer(function(answer) {
                                instance.pc.setLocalDescription(answer, function() {
                                    console.log("created Answer and setLocalDescription " + JSON.stringify(answer));
                                    instance.msgs.push({
                                        type: context.signallingMessageType.sessionDescription,
                                        args: JSON.stringify(answer)
                                    });
                                }, context.error);
                            }, context.error);
                        }, context.error);
                        break;
                }
                break;
            case context.signallingMessageType.iceCandidate:
                // Workaround for chrome issue, instruct calling context to
                // buffer if remote description not yet sent
                // https://stackoverflow.com/questions/38198751
                if (!instance.pc.remoteDescription) {
                    return false;
                }
                instance.pc.addIceCandidate(JSON.parse(UTF8ToString(args)))
                .catch((error) => {
                    console.warn("Ignoring ice candidate after add fail: " + error);
                });
                break;
        }

        return true;
    },

    JS_WebRTC_GetIceConnectionState: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return -1;
        }

        switch(instance.pc.iceConnectionState)
        {
            case "closed": return context.iceConnectionState.closed;
            case "failed": return context.iceConnectionState.failed;
            case "disconnected": return context.iceConnectionState.disconnected;
            case "new": return context.iceConnectionState.new;
            case "checking": return context.iceConnectionState.checking;
            case "connected": return context.iceConnectionState.connected;
            default: return -1;
        }
    },

    JS_WebRTC_GetPeerConnectionState: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return -1;
        }

        switch(instance.pc.connectionState)
        {
            case "closed": return context.peerConnectionState.closed;
            case "failed": return context.peerConnectionState.failed;
            case "disconnected": return context.peerConnectionState.disconnected;
            case "new": return context.peerConnectionState.new;
            case "connecting": return context.peerConnectionState.connecting;
            case "connected": return context.peerConnectionState.connected;
            default: return -1;
        }
    },

    JS_WebRTC_SignallingMessages_Has: function(id) {
        let instance = context.instances[id];
        return instance && instance.msgs.length > 0;
    },

    JS_WebRTC_SignallingMessages_GetArgs: function(id) {
        let instance = context.instances[id];
        if (!instance || instance.msgs.length === 0) {
            return -1;
        }

        let args = instance.msgs[instance.msgs.length-1].args;
        let bufferSize = lengthBytesUTF8(args) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(args, buffer, bufferSize);
        return buffer;
    },

    JS_WebRTC_SignallingMessages_GetType: function(id) {
        let instance = context.instances[id];
        if (!instance || instance.msgs.length === 0) {
            return -1;
        }

        return instance.msgs[instance.msgs.length-1].type;
    },

    JS_WebRTC_SignallingMessages_Pop: function(id) {
        let instance = context.instances[id];
        if (!instance) {
            return;
        }

        instance.msgs.pop();
    },

    JS_WebRTC_SetPanner: function(id, x, y, z) {
        let instance = context.instances[id];
        if (!instance || !instance.panner) {
            return;
        }

        instance.panner.positionX.value = x;
        instance.panner.positionY.value = y;
        instance.panner.positionZ.value = z;
    },

    JS_WebRTC_GetStatsSamples: function(id) {
        let instance = context.instances[id];
        if (!instance || instance.stats.samples === 0) {
            return 0;
        }

        return 16000 * (instance.stats.samples / instance.sampleRate);
    },

    JS_WebRTC_GetStatsVolume: function(id) {
        let instance = context.instances[id];
        if (!instance || instance.stats.samples === 0) {
            return 0;
        }

        return 16000 * (instance.stats.volume / instance.sampleRate);
    },

    JS_WebRTC_EndStats: function(id) {
        let instance = context.instances[id];
        if (!instance) {
            return;
        }

        instance.stats.volume = instance.volume;
        instance.stats.samples = instance.samples;
        instance.volume = 0;
        instance.samples = 0;
    }

    // // Setup the audiocontext and all required objects. Should be called before
    // // any of the other js microphone interface functions in this file. If this
    // // returns true, it is possible to start an audio recording with Start()
    // JS_Microphone_InitOrResumeContext: function() {
    //     if (!WEBAudio || WEBAudio.audioWebEnabled == 0) {
    //         // No WEBAudio object (Unity version changed?)
    //         return false;
    //     }

    //     navigator.getUserMedia =
    //         navigator.getUserMedia || navigator.webkitGetUserMedia ||
    //         navigator.mozGetUserMedia || navigator.msGetUserMedia;
    //     if (!navigator.getUserMedia){
    //         return false;
    //     }

    //     var ctx = document.unityMicrophoneInteropContext;
    //     if (!ctx) {
    //         document.unityMicrophoneInteropContext = {};
    //         ctx = document.unityMicrophoneInteropContext;
    //     }

    //     if (!ctx.audioContext || ctx.audioContext.state == "closed"){
    //         ctx.audioContext = new AudioContext();
    //     }

    //     if (ctx.audioContext.state == "suspended") {
    //         ctx.audioContext.resume();
    //     }

    //     if (ctx.audioContext.state == "suspended") {
    //         return false;
    //     }

    //     return true;
    // },

    // // Returns the index of the most recently created audio clip so we can
    // // write to it. Should be called immediately after creating the clip and
    // // the value stored for indexing purposes.
    // // Relies on undocumented/unexposed js code within Unity's WebGL code,
    // // so may break with later versions
    // JS_Microphone_GetBufferInstanceOfLastAudioClip: function() {
    //     if (WEBAudio && WEBAudio.audioInstanceIdCounter) {
    //         return WEBAudio.audioInstanceIdCounter;
    //     }
    //     return -1;
    // },

    // JS_Microphone_IsRecording: function(deviceIndex) {
    //     var ctx = document.unityMicrophoneInteropContext;
    //     return ctx && ctx.stream;
    // },

    // // Get the current index of the last recorded sample
    // JS_Microphone_GetPosition: function(deviceIndex) {
    //     var ctx = document.unityMicrophoneInteropContext;
    //     if (ctx && ctx.stream) {
    //         return ctx.stream.currentPosition;
    //     }
    //     return -1;
    // },

    // // Get the sample rate for this device
    // // According to https://www.w3.org/TR/webaudio/ WebAudio implementations
    // // must support 8khz to 96khz. In practice seems to be best to let the
    // // browser pick the sample rate it prefers to avoid audio glitches
    // JS_Microphone_GetSampleRate: function(deviceIndex) {
    //     var ctx = document.unityMicrophoneInteropContext;
    //     if (ctx && ctx.audioContext.state == "running") {
    //         return ctx.audioContext.sampleRate;
    //     }
    //     return -1;
    // },

    // // Note samplesPerUpdate balances performance against latency, must be one of:
    // // 256, 512, 1024, 2048, 4096, 8192, 16384
    // // Note also that the clip sample count for the buffer instance should be
    // // a multiple of samplesPerUpdate
    // JS_Microphone_Start: function(deviceIndex,bufferInstance,samplesPerUpdate) {
    //     var ctx = document.unityMicrophoneInteropContext;
    //     if (ctx && ctx.stream) {
    //         // We are already recording
    //         return false;
    //     }

    //     var sound = WEBAudio.audioInstances[bufferInstance];
    //     if (!sound || !sound.buffer) {
    //         // No buffer for the given bufferInstance (Unity version changed?)
    //         return false;
    //     }

    //     var outputArray = sound.buffer.getChannelData(0);
    //     var outputArrayLen = outputArray.length;

    //     navigator.getUserMedia(
    //         {audio:true},
    //         function(userMediaStream) {
    //             var stream = {};
    //             stream.userMediaStream = userMediaStream;
    //             stream.microphoneSource = ctx.audioContext.createMediaStreamSource(userMediaStream);
    //             stream.processorNode = ctx.audioContext.createScriptProcessor(samplesPerUpdate, 1, 1);
    //             stream.currentPosition = 0;
    //             stream.processorNode.onaudioprocess = function(event) {

    //                 // Simple version for minimum delay
    //                 // Assumes clip length is a multiple of samplesPerUpdate
    //                 var inputArray = event.inputBuffer.getChannelData(0);
    //                 var pos = stream.currentPosition;
    //                 outputArray.set(inputArray,pos);
    //                 pos = (pos + samplesPerUpdate) % outputArrayLen;
    //                 stream.currentPosition = pos;
    //             }

    //             stream.microphoneSource.connect(stream.processorNode);

    //             // Add a zero gain node and connect to destination
    //             // Some browsers seem to ignore a solo processor node
    //             stream.gainNode = new GainNode(ctx.audioContext,{gain:0});
    //             stream.processorNode.connect(stream.gainNode);
    //             stream.gainNode.connect(ctx.audioContext.destination);

    //             ctx.stream = stream;
    //         },
    //         function(e) {
    //             alert('Error capturing audio.');
    //         }
    //     );
    // },

    // JS_Microphone_End: function(deviceIndex) {
    //     var ctx = document.unityMicrophoneInteropContext;
    //     if (ctx && ctx.stream) {
    //         ctx.stream.userMediaStream.getTracks().forEach(
    //             function(track) {
    //                 track.stop();
    //             }
    //         );

    //         ctx.stream.gainNode.disconnect();
    //         ctx.stream.processorNode.disconnect();
    //         ctx.stream.microphoneSource.disconnect();

    //         delete ctx.stream;
    //     }
    // }
};

autoAddDeps(unityWebRtcInteropLibrary, '$context');
mergeInto(LibraryManager.library, unityWebRtcInteropLibrary);