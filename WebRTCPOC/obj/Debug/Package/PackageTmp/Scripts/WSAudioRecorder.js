﻿
//NOTE, Websockets in workers ONLY WORKS IN CHROME. Script will have to be modified to work in FireFox.
(function (window) {

    var WORKER_PATH = root + '/scripts/audioWorker.js';

    //function convertFloat32ToInt16(buffer) {
    //    l = buffer.length;
    //    buf = new Int16Array(l);
    //    while (l--) {
    //        buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    //    }
    //    return buf.buffer;
    //}

    /**
    * Most of this code is copied wholesale from https://github.com/mattdiamond/Recorderjs
    * This is not Stereo, on the right channel is grabbed but that is enough for me
    */
    var WSAudioRecorder = function (source, wsURL, peerId) {
        var recording = false;
        
        var worker = new Worker(WORKER_PATH + '?' + (Math.random() * 1000000));
       
        var config = {};
        var bufferLen = 4096;
 

        this.context = source.context;
        this.node = (this.context.createScriptProcessor ||
                     this.context.createJavaScriptNode).call(this.context,
                                                             bufferLen, 2, 2);
        this.node.onaudioprocess = function (e) {
            if (!recording) return;
            var sample = e.inputBuffer.getChannelData(0);
            //Moved the float to 16 bit translation down to codebehind
             
            worker.postMessage({
                command: 'record',
                samples: sample
            });
        }

        this.record = function () {
            recording = true;
        }
        var that = this;
        worker.onmessage = function (msg) {
            if (msg.data === "Id-Received") {
                that.record();
            }
        };
        worker.postMessage({
            command: 'init',
            config: {
                uri: wsURL, peerId: peerId
            }
        });

        this.stop = function () {
            recording = false;
            worker.postMessage({
                command: 'stop',
            });
        }

        this.isRecording = function () {
            return recording;
        }

        source.connect(this.node);
        this.node.connect(this.context.destination);    //this should not be necessary, but it is
    };
    window.WSAudioRecorder = WSAudioRecorder;
})(window);