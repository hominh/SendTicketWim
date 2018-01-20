using ProtoBuf;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TramCanCoDinh
{
    public class SendData
    {
        IModel _model;
        private string ExchangeName = "";
        private string RoutingKey = "";
        ConnectionFactory Factory = new ConnectionFactory
        {
            HostName = ConfigAccess.GetIpServer(),
            UserName = ConfigAccess.GetUserName(),
            Password = ConfigAccess.GetPassword(),
            Port = ConfigAccess.GetPortServer(),
            VirtualHost = ConfigAccess.GetVirtualHost(),
            Protocol = Protocols.DefaultProtocol
        };

        public SendData() {
            this.ExchangeName = ConfigAccess.GetExchange();
            this.RoutingKey = ConfigAccess.GetRoutingKey();
            _model = Factory.CreateConnection().CreateModel();
        }

        public SendData(string exchangeName)
        {
            this.ExchangeName = exchangeName;
            _model = Factory.CreateConnection().CreateModel();
        }

        PhieuCanEntity Deserialize(byte[] body)
        {
            using (var stream = new MemoryStream(body))
            {
                return Serializer.Deserialize<PhieuCanEntity>(stream);
            }
        }

        byte[] Serialize(PhieuCanEntity body)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, body);
                stream.Position = 0;
                return stream.ToArray();
            }
        }

        public void PushMessage(PhieuCanEntity message)
        {
            if (!_model.IsOpen)
            {
                this.ExchangeName = ConfigAccess.GetExchange();
                this.RoutingKey = ConfigAccess.GetRoutingKey();
                _model = Factory.CreateConnection().CreateModel();
            }
            _model.BasicPublish(ExchangeName, RoutingKey, null, Serialize(message));
        }
    }
}
