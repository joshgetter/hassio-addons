ARG BUILD_FROM
FROM $BUILD_FROM

ENV LANG C.UTF-8

# Copy data for add-on
COPY run.sh /
COPY Controller /Controller
RUN chmod a+x /run.sh

# Install dependencies
RUN apk add --no-cache \
    ffmpeg \
    nginx \
    nginx-mod-rtmp \
    python3 \
    py-pip

# Install python dependencies
RUN pip install -r /Controller/requirements.txt

# Nginx setup
COPY nginx/nginx.conf /etc/nginx/nginx.conf
RUN mkdir /run/nginx
RUN mkdir -p /tmp/streaming/thumbnails
RUN mkdir /tmp/streaming/hls

CMD [ "/run.sh" ]
#CMD ["python3", "/Controller/Controller.py"]
#CMD ["tail", "-f", "/dev/null"]