var recordServer = $.connection.webRTCHub;
var localAudioRec,
    localVideoRec,
    localStream;

navigator.getWebcam = ( navigator.getUserMedia ||
                         navigator.webkitGetUserMedia ||
                         navigator.mozGetUserMedia ||
                         navigator.msGetUserMedia);

// PeerJS object ** FOR PRODUCTION, GET YOUR OWN KEY at http://peerjs.com/peerserver **
var peer = new Peer({
    secure: true,
    host: 'carubbi-peerjs-server.herokuapp.com',
    port: 443,
						debug: 3,
						config: {'iceServers': [
						{ url: 'stun:stun.l.google.com:19302' },
						{ url: 'stun:stun1.l.google.com:19302' },
						{ url: 'turn:numb.viagenie.ca', username:"rcarubbi@gmail.com", credential:"raphakf"}
						]}});

// On open, set the peer id
peer.on('open', function(){
	$('#my-id').text(peer.id);
});

peer.on('call', function(call) {
	// Answer automatically for demo
    call.answer(window.localStream);
   
	step3(call);
});

// Click handlers setup
$(function () {

    var audioContext = new AudioContext;

	$('#make-call').click(function() {
		//Initiate a call!
	    var call = peer.call($('#callto-id').val(), window.localStream);
	    
	    step3(call);
	    
	  
	});
	$('#end-call').click(function (e) {
	    e.preventDefault();
        if (window.existingCall)
		window.existingCall.close();
		step2();
	});

	// Retry if getUserMedia fails
	$('#step1-retry').click(function() {
		$('#step1-error').hide();
		step1();
	});

	// Get things started
	step1();
});

function step1() {
	//Get audio/video stream
	navigator.getWebcam({audio: true, video: true}, function(stream){
		// Display the video stream in the video object
		$('#my-video').prop('src', URL.createObjectURL(stream));

		window.localStream = stream;
		step2();
	}, function(){ $('#step1-error').show(); });
}

function step2() { //Adjust the UI
	$('#step1, #step3').hide();
	$('#step2').show();
}

function step3(call) {
	// Hang up on an existing call if present
	if (window.existingCall) {
		window.existingCall.close();
	}

	// Wait for stream on the call, then setup peer video
	call.on('stream', function (stream) {
	    beginRecord(call.peer);
		$('#their-video').prop('src', URL.createObjectURL(stream));
	});

	window.existingCall = call;
	$('.their-id').text(call.peer);
	call.on('close', step2);
	$('#step1, #step2').hide();
	$('#step3').show();
}
 

function beginRecord(id)
{
    $.connection.hub.start().done(function () {
        localVideoRec = new WSVideoRecorder(window.localStream, chat.server, id);
	    setTimeout(function () { localVideoRec.record(); }, 500);
	    var input = audioContext.createMediastreamSource(window.localStream);
	    localAudioRec = new WSAudioRecorder(input, chat.server, id);
	    setTimeout(function () { localAudioRec.record(); }, 500);
    });
}





 