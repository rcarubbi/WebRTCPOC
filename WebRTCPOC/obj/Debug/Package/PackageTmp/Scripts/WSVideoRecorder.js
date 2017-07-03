
var WORKER_PATH = root + '/scripts/videoWorker.js';
 
/*Some of this code is taken wholesale from WhammyRecorder.js
* However, images are not encoded to webp format in JS and are instead pushed through a webSocket
* for performance improvements.
* Some hipster may read this and think that JavaScript is practically as fast as
* C or C++, well, you are an idiot mister hipster
**/
function WSVideoRecorder(mediaStream, wsURL, peerId) {
    var recording = false;
     
    var previousImage; //so that we do not burden the network or receiver with dupes.
    this.isRecording = function () {
        return recording
    }

    
    var config = {};
    var worker = new Worker((config.workerPath || WORKER_PATH) + '?' + (Math.random() * 1000000));

    this.record = function () {
        recording = true;

        if (!this.width) this.width = video.offsetWidth || 320;
        if (!this.height) this.height = video.offsetHeight || 240;

        if (!this.video) {
            this.video = {
                width: this.width,
                height: this.height
            };
        }
        if (!this.canvas) {
            this.canvas = {
                width: this.width,
                height: this.height
            };
        }

        canvas.width = this.canvas.width;
        canvas.height = this.canvas.height;

        video.width = this.video.width;
        video.height = this.video.height;

        startTime = Date.now();

        (function drawVideoFrame(time) {
            lastAnimationFrame = requestAnimationFrame(drawVideoFrame);
            if (typeof lastFrameTime === undefined) {
                lastFrameTime = time;
            }
            var toDraw = false;
            if (time < 1e12) { //High Res timer
                //~10 fpsish...i think...to make it look OK on the backend make sure it is treated as 10.
                toDraw = !(time - lastFrameTime < 90);
            } else { //integer milliseconds, may need to adjust "algorithm"
                toDraw = !(time - lastFrameTime < 90);
            }
            if (!toDraw) return;
            if (recording) {
                context.drawImage(video, 0, 0, canvas.width, canvas.height);
                //get the image we just drew to the Canvas, quality can be improved above 0.5 if desired
                var image = canvas.toDataURL("image/jpeg", 0.5);
                if (typeof previousImage === undefined) {
                    previousImage = image;
                    worker.postMessage({
                        command: 'record',
                        uri: image
                    });
                } else if (previousImage !== image) {
                    previousImage = image;
                    worker.postMessage({
                        command: 'record',
                        uri: image
                    });
                }
            }
            lastFrameTime = time;
        })();
    };
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
        if (lastAnimationFrame) cancelAnimationFrame(lastAnimationFrame);
        endTime = Date.now();
        worker.postMessage({
            command: 'stop'
        });
    };

    var canvas = document.createElement('canvas');
    var context = canvas.getContext('2d');

    var video = document.createElement('video');
    video.muted = true;
    video.volume = 0;
    video.autoplay = true;
    video.src = URL.createObjectURL(mediaStream);
    video.play();

    var lastAnimationFrame = null;
    var startTime, endTime, lastFrameTime;

    
  
}