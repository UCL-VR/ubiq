var unityWebRtcInteropLibrary = {
    $context: {
        instances: {},

        lastId: 0,

        audioContext: null,
        whenUserMedia: null,

        // For record stats
        processorNode: null,
        gainNode: null,
        src: null,
        record: {
            lastFrame: -1,
            volumeSum: 0,
            sampleCount: 0,
            sampleRate: 0,
            stats: {
                sampleCount: 0,
                volumeSum: 0
            }
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
            console.error("Closing this Peer Connection. Reason: " + e);
        }
    },

    JS_WebRTC_New: function() {
        let id = context.lastId++;
        context.instances[id] = {
            startArgs: {
                polite: false,
                iceServers: [],
            },
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

        instance.polite = polite;
        instance.msgs = [];
        instance.playback = {
            volumeSum: 0,
            sampleCount: 0,
            sampleRate: 0,
            stats: {
                sampleCount: 0,
                volumeSum: 0
            }
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
        });

        instance.pc = new RTCPeerConnection({ iceServers: iceServers });
        instance.pc.ontrack = ({track}) => {
            track.onunmute = () => {
                // Skip if we've already performed this setup
                if (instance.src) return;

                let stream = new MediaStream([track]);

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

                // Setup processing for volume stats and panning for 3d audio
                let audioContext = context.audioContext;
                instance.processorNode = audioContext.createScriptProcessor(4096, 1, 1);
                instance.processorNode.onaudioprocess = ({inputBuffer}) => {
                    let arr = inputBuffer.getChannelData(0);
                    let length = arr.length;
                    for (let i = 0; i < length; i++) {
                        instance.playback.volumeSum += Math.abs(arr[i]);
                    }
                    instance.playback.sampleCount += length;
                    instance.playback.sampleRate = inputBuffer.sampleRate;
                };
                instance.gainNode = new GainNode(audioContext,{gain:0});
                instance.src = audioContext.createMediaStreamSource(stream);
                instance.src.connect(instance.processorNode);
                instance.src.connect(instance.panner);
                instance.processorNode.connect(instance.gainNode);
                instance.gainNode.connect(audioContext.destination);
                instance.panner.connect(audioContext.destination);
            };
        };

        // Negotiation strategy: w3c.github.io/webrtc-pc/#perfect-negotiation-example
        instance.makingOffer = false;
        instance.ignoreOffer = false;
        instance.isSettingRemoteAnswerPending = false;

        instance.pc.onicecandidate = ({candidate}) => {
            instance.msgs.push({candidate});
        };

        instance.pc.onnegotiationneeded = async () => {
            try {
                instance.makingOffer = true;
                await instance.pc.setLocalDescription();
                instance.msgs.push({description: instance.pc.localDescription});
            }
            catch (err) {
                console.error(err);
            }
            finally {
                instance.makingOffer = false;
            }
        };
        // Negotiation continued in JS_WebRTC_ProcessSignalingMessage

        context.whenUserMedia
        .then((stream) => {
            let instance = context.instances[id];
            if (!instance) {
                // Close() called before getUserMedia returned
                return;
            }

            if (!context.processorNode) {
                let audioContext = context.audioContext;
                context.processorNode = audioContext.createScriptProcessor(1024, 1, 1);
                context.processorNode.onaudioprocess = ({inputBuffer}) => {
                    let arr = inputBuffer.getChannelData(0);
                    let length = arr.length;
                    for (let i = 0; i < length; i++) {
                        context.record.volumeSum += Math.abs(arr[i]);
                    }
                    context.record.sampleCount += length;
                    context.record.sampleRate = inputBuffer.sampleRate;
                };
                context.gainNode = new GainNode(audioContext,{gain:0});
                context.src = audioContext.createMediaStreamSource(stream);
                context.src.connect(context.processorNode);
                context.processorNode.connect(context.gainNode);
                context.gainNode.connect(audioContext.destination);
            }

            for (const track of stream.getTracks()) {
                instance.pc.addTrack(track,stream);
            }
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

    JS_WebRTC_IsStarted: function(id) {
        let instance = context.instances[id];
        return Boolean(instance && instance.pc);
    },

    JS_WebRTC_SetPolite: function(id,polite) {
        let instance = context.instances[id];
        if (!instance) {
            return;
        }

        instance.polite = polite;
    },

    JS_WebRTC_ProcessSignalingMessage: async function (id,_candidate,sdpMid,
        sdpMLineIndexIsNull,sdpMLineIndex,usernameFragment,type,sdp) {

        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return;
        }

        let description = type ? {
            type: UTF8ToString(type),
            sdp: UTF8ToString(sdp),
        } : null;
        let candidate = _candidate ? {
            candidate: UTF8ToString(_candidate),
            sdpMid: UTF8ToString(sdpMid),
            sdpMLineIndex: sdpMLineIndexIsNull ? null : sdpMLineIndex,
            usernameFragment: UTF8ToString(usernameFragment),
        } : null;

        // w3c.github.io/webrtc-pc/#perfect-negotiation-example
        try {
            if (description) {
                // An offer may come in while we are busy processing SRD(answer).
                // In this case, we will be in "stable" by the time the offer is processed
                // so it is safe to chain it on our Operations Chain now.
                const readyForOffer =
                    !instance.makingOffer &&
                    (instance.pc.signalingState == "stable" || instance.isSettingRemoteAnswerPending);
                const offerCollision = description.type == "offer" && !readyForOffer;

                instance.ignoreOffer = !instance.polite && offerCollision;
                if (instance.ignoreOffer) {
                    return;
                }
                instance.isSettingRemoteAnswerPending = description.type == "answer";
                await instance.pc.setRemoteDescription(description); // SRD rolls back as needed
                instance.isSettingRemoteAnswerPending = false;
                if (description.type == "offer") {
                    await instance.pc.setLocalDescription();
                    instance.msgs.push({description: instance.pc.localDescription});
                }
            } else if (candidate) {
                try {
                    await instance.pc.addIceCandidate(candidate);
                } catch (err) {
                    if (!instance.ignoreOffer) throw err; // Suppress ignored offer's candidates
                }
            }
        } catch (err) {
            console.error(err);
        }
    },

    // Workaround for chrome issue, buffer candidates if remote desc
    // not yet set https://stackoverflow.com/questions/38198751
    JS_WebRTC_HasRemoteDescription: function(id) {
        let instance = context.instances[id];
        return Boolean(instance && instance.pc && instance.pc.remoteDescription);
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

    // Notes on this SignalingMessage code:
    // =====================================
    // Longwinded workaround for Unity's JsonUtility lack of support for null
    // values. Browser-to-browser, just sending the JSON would be fine, but
    // we want this implementation to play nicely with the WebRTC plugins for
    // other platforms, which we've written with JsonUtility. So we pass the
    // required variables up to be handled by the workaround we on the C# side.

    // Could bundle another JSON lib but good to avoid the dependency.

    // Note about the mallocs here: Unity assures us that the memory will be
    // automatically freed so long as the string is a return value:
    // https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html

    JS_WebRTC_SignalingMessages_Has: function(id) {
        let instance = context.instances[id];
        return instance && instance.pc && instance.msgs.length > 0;
    },

    JS_WebRTC_SignalingMessages_GetCandidate: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc || instance.msgs.length === 0
            || !instance.msgs[instance.msgs.length-1].candidate) {
            return null;
        }

        let candidate = instance.msgs[instance.msgs.length-1].candidate.candidate;
        let bufferSize = lengthBytesUTF8(candidate) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(candidate, buffer, bufferSize);
        return buffer;
    },

    JS_WebRTC_SignalingMessages_GetSdpMid: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc || instance.msgs.length === 0
            || !instance.msgs[instance.msgs.length-1].candidate
            || !instance.msgs[instance.msgs.length-1].candidate.sdpMid) {
            return null;
        }

        let sdpMid = instance.msgs[instance.msgs.length-1].candidate.sdpMid;
        let bufferSize = lengthBytesUTF8(sdpMid) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(sdpMid, buffer, bufferSize);
        return buffer;
    },

    JS_WebRTC_SignalingMessages_GetSdpMLineIndex: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc || instance.msgs.length === 0
            || !instance.msgs[instance.msgs.length-1].candidate
            || !instance.msgs[instance.msgs.length-1].candidate.sdpMLineIndex) {
            return -1;
        }

        return Number(instance.msgs[instance.msgs.length-1].candidate.sdpMLineIndex);
    },

    JS_WebRTC_SignalingMessages_GetUsernameFragment: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc || instance.msgs.length === 0
            || !instance.msgs[instance.msgs.length-1].candidate
            || !instance.msgs[instance.msgs.length-1].candidate.usernameFragment) {
            return null;
        }

        let usernameFragment = instance.msgs[instance.msgs.length-1].candidate.usernameFragment;
        let bufferSize = lengthBytesUTF8(usernameFragment) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(usernameFragment, buffer, bufferSize);
        return buffer;
    },

    JS_WebRTC_SignalingMessages_GetType: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc || instance.msgs.length === 0
            || !instance.msgs[instance.msgs.length-1].description
            || !instance.msgs[instance.msgs.length-1].description.type) {
            return null;
        }

        let type = instance.msgs[instance.msgs.length-1].description.type;
        let bufferSize = lengthBytesUTF8(type) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(type, buffer, bufferSize);
        return buffer;
    },

    JS_WebRTC_SignalingMessages_GetSdp: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc || instance.msgs.length === 0
            || !instance.msgs[instance.msgs.length-1].description
            || !instance.msgs[instance.msgs.length-1].description.sdp) {
            return null;
        }

        let sdp = instance.msgs[instance.msgs.length-1].description.sdp;
        let bufferSize = lengthBytesUTF8(sdp) + 1;
        let buffer = _malloc(bufferSize);
        stringToUTF8(sdp, buffer, bufferSize);
        return buffer;
    },

    JS_WebRTC_SignalingMessages_Pop: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return;
        }

        instance.msgs.pop();
    },

    JS_WebRTC_SetPanner: function(id, x, y, z) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return;
        }

        instance.panner.positionX.value = x;
        instance.panner.positionY.value = y;
        instance.panner.positionZ.value = z;
    },

    JS_WebRTC_GetPlaybackStatsSampleCount: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return 0;
        }

        return instance.playback.stats.sampleCount;
    },

    JS_WebRTC_GetPlaybackStatsVolumeSum: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return 0;
        }

        return instance.playback.stats.volumeSum;
    },

    JS_WebRTC_GetPlaybackStatsSampleRate: function(id) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return 0;
        }

        return instance.playback.sampleRate;
    },

    JS_WebRTC_GetRecordStatsSampleCount: function() {
        return context.record.stats.sampleCount;
    },

    JS_WebRTC_GetRecordStatsVolumeSum: function() {
        return context.record.stats.volumeSum;
    },

    JS_WebRTC_GetRecordStatsSampleRate: function() {
        return context.record.sampleRate;
    },

    JS_WebRTC_EndStats: function(id, frameCount) {
        let instance = context.instances[id];
        if (!instance || !instance.pc) {
            return;
        }

        instance.playback.stats.volumeSum = instance.playback.volumeSum;
        instance.playback.stats.sampleCount = instance.playback.sampleCount;
        instance.playback.volumeSum = 0;
        instance.playback.sampleCount = 0;

        if (frameCount != context.record.lastFrame) {
            context.record.stats.volumeSum = context.record.volumeSum;
            context.record.stats.sampleCount = context.record.sampleCount;
            context.record.volumeSum = 0;
            context.record.sampleCount = 0;
            context.record.lastFrame = frameCount;
        }
    }
};

autoAddDeps(unityWebRtcInteropLibrary, '$context');
mergeInto(LibraryManager.library, unityWebRtcInteropLibrary);