

function makePeerHeartbeater(peer) {
    var timeoutId = 0;
    function heartbeat() {
        timeoutId = setTimeout(heartbeat, 20000);
        if (peer.socket._wsOpen()) {
            peer.socket.send({ type: 'HEARTBEAT' });
        }
    }
    // Start 
    heartbeat();
    // return
    return {
        start: function () {
            if (timeoutId === 0) { heartbeat(); }
        },
        stop: function () {
            clearTimeout(timeoutId);
            timeoutId = 0;
        }
    };
}
var userInfoAPI = root + "/api/User";
var localStream;
var heartbeater;

navigator.getWebcam = (navigator.getUserMedia ||
    navigator.webkitGetUserMedia ||
    navigator.mozGetUserMedia ||
    navigator.msGetUserMedia);

// PeerJS object ** FOR PRODUCTION, GET YOUR OWN KEY at http://peerjs.com/peerserver **
var peer = new Peer({
    peer: $('#my-id').text(),
    secure: true,
    host: 'carubbi-peerjs-server.herokuapp.com',
    port: 443,
    debug: 3,
    config: {
        'iceServers': [
            { url: 'stun:stun.l.google.com:19302' },
            { url: 'stun:stun1.l.google.com:19302' },
            { url: 'turn:numb.viagenie.ca', username: "rcarubbi@gmail.com", credential: "raphakf" }
        ]
    }
});

// On open, set the peer id
peer.on('open', function () {

    $.post(userInfoAPI, { name: $('#my-id').text(), id: peer.id });
    heartbeater = makePeerHeartbeater(peer);
});

peer.on('call', function (call) {
    // Answer automatically for demo
    call.answer(window.localStream);
    startRecording();
    step3(call);
});


var server = null;
if (window.location.protocol === 'http:')
    server = "http://" + window.location.hostname + ":8088/janus";
else
    server = "https://" + window.location.hostname + ":8089/janus";

var janus = null;
var recordplay = null;
var bandwidth = 1024 * 1024;


var myname = null;
var recording = false;
var playing = false;
var recordingId = null;
var selectedRecording = null;
var selectedRecordingInfo = null;

// Click handlers setup
$(function () {

   

    $('#make-call').click(function () {
        //Initiate a call!
        $.get(userInfoAPI, { name: $('#callto-id').val(), id: "" }, function (id) {
            var call = peer.call(id, window.localStream);
            step3(call);
            startRecording();
        });
    });
    $('#end-call').click(function (e) {
        e.preventDefault();
        if (window.existingCall)
            window.existingCall.close();
        stop();
        step2();
    });

    // Retry if getUserMedia fails
    $('#step1-retry').click(function () {
        $('#step1-error').hide();
        step1();
    });

    // Get things started
    step1();
});

function step1() {
    //Get audio/video stream
    navigator.getWebcam({ audio: true, video: true }, function (stream) {
        // Display the video stream in the video object
        $('#my-video').prop('src', URL.createObjectURL(stream));

        Janus.init({
            debug: "all",
            callback: function () {
                janus = new Janus(
                    {
                        server: server,
                        success: janusSuccess,
                        error: janusError,
                        destroyed: janusDestroyed
                    });
            }
        });

        window.localStream = stream;
        step2();
    }, function () { $('#step1-error').show(); });
}

function step2() { //Adjust the UI
    $('#step1, #step3').hide();
    $('#step2').show();
}

function step3(call) {
    // Hang up on an existing call if present
    if (window.existingCall) {
        window.existingCall.close();
        stop();
    }

    // Wait for stream on the call, then setup peer video
    call.on('stream', function (stream) {
        $('#their-video').prop('src', URL.createObjectURL(stream));
    });

    window.existingCall = call;
    $.get(userInfoAPI, { id: call.peer, name: "" }, function (name) {
        $('.their-id').text(name);
    });

    call.on('close', function () {
        stop();
        step2();
    });

    $('#step1, #step2').hide();
    $('#step3').show();
}


function now() {
    var today = new Date();
    var dd = today.getDate();
    var mm = today.getMonth() + 1; //January is 0!

    var yyyy = today.getFullYear();
    if (dd < 10) {
        dd = '0' + dd;
    }
    if (mm < 10) {
        mm = '0' + mm;
    }
    var today = '' + yyyy + mm + dd
    return today;
}
// gravação

function startRecording() {
    if (recording)
        return;
    // Start a recording
    recording = true;
    playing = false;
    myname = $('#my-id').text() + now()

    // bitrate and keyframe interval can be set at any time: 
    // before, after, during recording
    recordplay.send({
        'message': {
            'request': 'configure',
            'video-bitrate-max': bandwidth, // a quarter megabit
            'video-keyframe-interval': 15000 // 15 seconds
        }
    });

    recordplay.createOffer(
        {
            // By default, it's sendrecv for audio and video...
            success: function (jsep) {
                Janus.debug("Got SDP!");
                Janus.debug(jsep);
                var body = { "request": "record", "name": myname };
                recordplay.send({ "message": body, "jsep": jsep });
            },
            error: function (error) {
                Janus.error("WebRTC error...", error);
                recordplay.hangup();
            }
        });

}

function stop() {
    // Stop a recording/playout
    var stop = { "request": "stop" };
    recordplay.send({ "message": stop });
    recordplay.hangup();
}

function janusSuccess() {

    // Attach to echo test plugin
    janus.attach(
        {
            plugin: "janus.plugin.recordplay",
            success: attachPluginRecordPlaySuccess,
            error: attachPluginRecordPlayError,
            consentDialog: consentDialog,
            webrtcState: webrtcState,
            onmessage: recordPlayOnMessage,
            onlocalstream: function (stream) {
                if (playing === true)
                    return;
                Janus.debug(" ::: Got a local stream :::");
                Janus.debug(JSON.stringify(stream));

            },
            onremotestream: function (stream) {
                if (playing === false)
                    return;
                Janus.debug(" ::: Got a remote stream :::");
                Janus.debug(JSON.stringify(stream));
            },
            oncleanup: function () {
                Janus.log(" ::: Got a cleanup notification :::");
                // FIXME Reset status
                recording = false;
                playing = false;
            }
        });
}

function janusError(error) {
    Janus.error(error);
}

function janusDestroyed() {
    window.location.reload();
}

function attachPluginRecordPlaySuccess(pluginHandle) {
    recordplay = pluginHandle;
    Janus.log("Plugin attached! (" + recordplay.getPlugin() + ", id=" + recordplay.getId() + ")");
}

function attachPluginRecordPlayError(error) {
    Janus.error("  -- Error attaching plugin...", error);
}

function consentDialog(on) {
    Janus.debug("Consent dialog should be " + (on ? "on" : "off") + " now");
}

function webrtcState(on) {
    Janus.log("Janus says our WebRTC PeerConnection is " + (on ? "up" : "down") + " now");
}

function recordPlayOnMessage(msg, jsep) {
    Janus.debug(" ::: Got a message :::");
    Janus.debug(JSON.stringify(msg));
    var result = msg["result"];
    if (result !== null && result !== undefined) {
        if (result["status"] !== undefined && result["status"] !== null) {
            var event = result["status"];
            if (event === 'preparing') {
                Janus.log("Preparing the recording playout");
                recordplay.createAnswer(
                    {
                        jsep: jsep,
                        media: { audioSend: false, videoSend: false },	// We want recvonly audio/video
                        success: recordPlayCreateAnswerSuccess,
                        error: recordPlayCreateAnswerError
                    });
                if (result["warning"])
                    Janus.log(result["warning"]);
            } else if (event === 'recording') {
                // Got an ANSWER to our recording OFFER
                if (jsep !== null && jsep !== undefined)
                    recordplay.handleRemoteJsep({ jsep: jsep });
                var id = result["id"];
                if (id !== null && id !== undefined) {
                    Janus.log("The ID of the current recording is " + id);
                    recordingId = id;
                }
            } else if (event === 'slow_link') {
                var uplink = result["uplink"];
                if (uplink !== 0) {
                    // Janus detected issues when receiving our media, let's slow down
                    bandwidth = parseInt(bandwidth / 1.5);
                    recordplay.send({
                        'message': {
                            'request': 'configure',
                            'video-bitrate-max': bandwidth, // Reduce the bitrate
                            'video-keyframe-interval': 15000 // Keep the 15 seconds key frame interval
                        }
                    });
                }
            } else if (event === 'playing') {
                Janus.log("Playout has started!");
            } else if (event === 'stopped') {
                Janus.log("Session has stopped!");
                var id = result["id"];
                if (recordingId !== null && recordingId !== undefined) {
                    if (recordingId !== id) {
                        Janus.warn("Not a stop to our recording?");
                        return;
                    }
                }
                if (selectedRecording !== null && selectedRecording !== undefined) {
                    if (selectedRecording !== id) {
                        Janus.warn("Not a stop to our playout?");
                        return;
                    }
                }
                // FIXME Reset status
                recordingId = null;
                recording = false;
                playing = false;
                recordplay.hangup();
            }
        }
    } else {
        // FIXME Error?
        var error = msg["error"];

        // FIXME Reset status
        recording = false;
        playing = false;
        recordplay.hangup();
    }
}

function recordPlayCreateAnswerSuccess(jsep) {
    Janus.debug("Got SDP!");
    Janus.debug(jsep);
    var body = { "request": "start" };
    recordplay.send({ "message": body, "jsep": jsep });
}

function recordPlayCreateAnswerError(error) {
    Janus.error("WebRTC error:", error);

}