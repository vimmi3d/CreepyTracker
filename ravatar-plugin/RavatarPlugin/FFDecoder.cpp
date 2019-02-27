#include "FFDecoder.h"

FFDecoder::FFDecoder(std::string fn)
{
	// initialize libav

//	av_register_all();
	avformat_network_init();
	filename = fn;
	// initialize custom data structure
	_fmt_ctx = NULL;
	_stream_idx = -1;
	_video_stream = NULL;
	_codec_ctx = NULL;
	_decoder = NULL;
	_av_frame = NULL;
	_gl_frame = NULL;
	_conv_ctx = NULL;
	

}

bool FFDecoder::init() {
	// open video
	if (avformat_open_input(&_fmt_ctx, filename.c_str(), NULL, NULL) < 0) {
		return false;
	}

	// find stream info
	if (avformat_find_stream_info(_fmt_ctx, NULL) < 0) {
		return false;
	}

	// dump debug info
	av_dump_format(_fmt_ctx, 0, filename.c_str(), 0);

	// find the video stream
	for (unsigned int i = 0; i < _fmt_ctx->nb_streams; ++i)
	{
		if (_fmt_ctx->streams[i]->codecpar->codec_type == AVMEDIA_TYPE_VIDEO)
		{
			_stream_idx = i;
			break;
		}
	}

	if (_stream_idx == -1)
	{
		return false;
	}

	_video_stream = _fmt_ctx->streams[_stream_idx];
	//MUDEI AQUI
	AVCodec * input_codec;
	if (!(input_codec = avcodec_find_decoder(_fmt_ctx->streams[0]->codecpar->codec_id))) {
		fprintf(stderr, "Could not find input codec\n");
		avformat_close_input(&_fmt_ctx);
		return false;
	}
	_codec_ctx = avcodec_alloc_context3(input_codec);
	avcodec_parameters_to_context(_codec_ctx,_video_stream->codecpar);

	// find the decoder
	_decoder = avcodec_find_decoder(_codec_ctx->codec_id);
	if (_decoder == NULL)
	{
		return false;
	}

	// open the decoder
	if (avcodec_open2(_codec_ctx, _decoder, NULL) < 0)
	{
		return -1;
	}

	// allocate the video frames
	_av_frame = av_frame_alloc();
	_gl_frame = av_frame_alloc();
	int size = av_image_get_buffer_size(MY_AV_PIXEL_TYPE, _codec_ctx->width,_codec_ctx->height,1);
	uint8_t *internal_buffer = (uint8_t *)av_malloc(size * sizeof(uint8_t));
	//CHANGED THIS
	int t =av_image_fill_arrays(_gl_frame->data, _gl_frame->linesize,internal_buffer, MY_AV_PIXEL_TYPE
		,_codec_ctx->width, _codec_ctx->height,1);
	_packet = (AVPacket *)av_malloc(sizeof(AVPacket));

	//av_init_packet(_packet);
	
	/*glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
	glTexParameteri(MY_GL_TEXTURE_TYPE, GL_TEXTURE_WRAP_S, GL_REPEAT);
	glTexParameteri(MY_GL_TEXTURE_TYPE, GL_TEXTURE_WRAP_T, GL_REPEAT);
	glTexParameteri(MY_GL_TEXTURE_TYPE, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(MY_GL_TEXTURE_TYPE, GL_TEXTURE_MIN_FILTER, GL_LINEAR);*/

	return true;
}
FFDecoder::~FFDecoder()
{
	if (_av_frame) av_free(_av_frame);
	if (_gl_frame) av_free(_gl_frame);
	if (_packet) av_free(_packet);
	if (_codec_ctx) avcodec_close(_codec_ctx);
	if (_fmt_ctx) avformat_free_context(_fmt_ctx);
	//if (_colorBuffer) free(_colorBuffer);
}


bool FFDecoder::getVideoFrame() {
	
	do {
		if (av_read_frame(_fmt_ctx, _packet) < 0) {
			av_packet_unref(_packet);
			return false;
		}

		if (_packet->stream_index == _stream_idx) {
			int frame_finished = 0;

			if (avcodec_send_packet(_codec_ctx, _packet) < 0) {
				av_packet_unref(_packet);
				return false;
			}
			frame_finished = avcodec_receive_frame(_codec_ctx, _av_frame);
			if (frame_finished >= 0) {
				if (!_conv_ctx) {
					_conv_ctx = sws_getContext(_codec_ctx->width,
						_codec_ctx->height, _codec_ctx->pix_fmt,
						_codec_ctx->width, _codec_ctx->height, MY_AV_PIXEL_TYPE,
						SWS_BICUBIC, NULL, NULL, NULL);
				}
				int ret = sws_scale(_conv_ctx, _av_frame->data, _av_frame->linesize, 0,
					_codec_ctx->height, _gl_frame->data, _gl_frame->linesize);
			}
		}


		av_packet_unref(_packet);
	} while (_packet->stream_index != _stream_idx);

	return true;
}

bool FFDecoder::seekFrame(int frame) 
{
	if (av_seek_frame(_fmt_ctx, -1, frame, AVSEEK_FLAG_FRAME | AVSEEK_FLAG_ANY) >= 0)
		return true;
	
	return false;
	
}

int FFDecoder::getTotalFrames()
{
	return _video_stream->nb_frames;
}

int FFDecoder::getCurrentFrame()
{
	return _av_frame->display_picture_number;
}