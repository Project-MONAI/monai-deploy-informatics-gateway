#!/usr/bin/env python

import json
import logging
import os
import sys
from pathlib import Path
from types import SimpleNamespace

import pika
from minio import Minio
from monai.deploy.runner import runner


class App():

    def __init__(self) -> None:
        self._logger = logging.getLogger("demo")
        self._load_config()
        self._init_messaging()
        self._init_storage()

    def _init_storage(self) -> None:
        config = self._config['storage']
        self._storage_client = Minio(
            config['endpoint'],
            config['username'],
            config['password'],
            secure=False # DEMO purposes only!!! Make sure to use a secure connection!!!
        )

        if not self._storage_client.bucket_exists(config['bucket']):
            raise f"Bucket '{config['bucket']}' does not exist"
        
        if not os.path.exists(self._working_directory):
            self._logger.info(f"Creating working directory {self._working_directory}")
            os.makedirs(self._working_directory)

    def _init_messaging(self) -> None:
        config = self._config['messaging']
        credentials = pika.credentials.PlainCredentials(
            config['username'], config['password'])

        self._logger.info(f"Connecting to message broker at {config['host']}")
        self._pika_connection = pika.BlockingConnection(pika.ConnectionParameters(
            host=config['host'], credentials=credentials, virtual_host=config['virtual_host']))
        self._pika_channel = self._pika_connection.channel()
        self._pika_channel.exchange_declare(
            exchange=config['exchange'], exchange_type='topic')
        result = self._pika_channel.queue_declare('', exclusive=True)
        self.messaging_queue_name = result.method.queue
        self._pika_channel.queue_bind(
            exchange=config['exchange'], queue=self.messaging_queue_name, routing_key=config['topic'])
        self._pika_channel.basic_consume(
            queue=self.messaging_queue_name, on_message_callback=self._message_callback, auto_ack=False)

    def _load_config(self) -> None:
        with open('config.json', 'r') as f:
            self._config = json.load(f)
        
        self._working_directory = Path(self._config['working_dir'])
        self._application = self._config['application']

    def _message_callback(self, ch, method, properties, body):
        correlation_id = properties.correlation_id
        self._logger.info(
            f"Message received from application={properties.app_id}. Correlation ID={correlation_id}. Delivery tag={method.delivery_tag}. Topic={method.routing_key}")

        request_message = json.loads(body)
        print(" body\t%r" % (request_message))
        print(" properties\t%r" % (properties))

        job_dir = self._working_directory / correlation_id
        job_dir_input = job_dir / "input"
        job_dir_output = job_dir / "output"
        if not os.path.exists(job_dir_input):
            self._logger.info(f"Creating working directory for job {job_dir}")
            os.makedirs(job_dir_input)
        if not os.path.exists(job_dir_output):
            self._logger.info(f"Creating working directory for job {job_dir}")
            os.makedirs(job_dir_output)
            
        # note: in 0.1.1, the bucket name can be found inside body.payload[]
        bucket=self._config['storage']['bucket']
        file_list = self._storage_client.list_objects(bucket, prefix=request_message['payload_id'],
                              recursive=True)
        for file in file_list:
            if file.object_name.endswith('json'):
                self._logger.info(f'Skipping JSON file {file.object_name}...')
                continue
                
            self._logger.info(f'Downloading file {file.object_name}...')
            data = self._storage_client.get_object(bucket, file.object_name)
            file_path = job_dir_input / os.path.dirname(file.object_name)
            if not os.path.exists(file_path):
                self._logger.info(f"Creating directory {file_path}")
                os.makedirs(file_path)
            
            file_path = job_dir_input / file.object_name
            with open(file_path, 'wb') as file_data:
                for d in data.stream(32*1024):
                    file_data.write(d)
        
        self._logger.info(f"Finished download payload {request_message['payload_id']}...")
        
        argsd = {}
        argsd['map'] = self._application
        argsd['input'] = job_dir_input
        argsd['output'] =job_dir_output
        argsd['quiet'] = False

        
        self._logger.info(f"Launching application {self._application}...")
        args = SimpleNamespace(**argsd)
        runner.main(args)
        self._pika_channel.basic_ack(method.delivery_tag)

    def run(self):
        self._logger.info('[*] Waiting for logs. To exit press CTRL+C')
        self._pika_channel.start_consuming()


if __name__ == "__main__":
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s [%(levelname)s] %(message)s",
        handlers=[
            logging.StreamHandler(sys.stdout)
        ]
    )
    app = App()
    app.run()
