using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlertSystem
{
    public class Constants
    {
        #region RabbitMQ Connection

        public const string RabbitMQConnectionName = "AlertSystemClient";

        #endregion

        #region Exchange/Queue

        public const string NotificationExchangeName = "Notification";
        public const string BroadcastNotificationExchangeName = "BroadcastNotification";
        public const string EmailQueueName = "Email";
        public const string SMSQueueName = "SMS";
        public const string BroadcastQueueName = "BroadcastQueue";
        public const string BroadcastEmailQueueName = "BroadcastEmail";
        public const string BroadcastSMSQueueName = "BroadcastSMS";
        public const string SMSQueueRoutingKey = "SMS-Routing";
        public const string EmailQueueRoutingKey = "Email-Routing";
        public const string BroadcastSMSQueueRoutingKey = "BroadcastSMS-Routing";
        public const string BroadcastEmailQueueRoutingKey = "BroadcastEmail-Routing";

        #endregion
    }
}
