using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AlertSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Members

        private bool m_bBroadcastingAllowed = false;
        private IConnection m_rabbitMQConnection;
        private IModel m_emailChannel;
        private IModel m_broadcastEmailChannel;
        private IModel m_smsChannel;
        private IModel m_broadcastsmsChannel;

        #endregion

        #region Public Properties

        public ObservableCollection<Email> LstEmailReceived { get; set; } = new ObservableCollection<Email>();
        public ObservableCollection<SMS> LstSmsReceived { get; set; } = new ObservableCollection<SMS>();

        #endregion
        public MainWindow()
        {
            InitializeComponent();
            grdEmail.DataContext = this;
            grdSMS.DataContext = this;
        }

        private void ConnectRabbitMQ_Click(object sender, RoutedEventArgs e)
        {
            string stConnectionString = ConfigurationManager.ConnectionStrings["RabbitMQConnection"].ConnectionString;

            var connectionFactory = new ConnectionFactory();

            connectionFactory.Uri = new System.Uri(stConnectionString);

            //due to some reason connection lost it make connection again
            connectionFactory.AutomaticRecoveryEnabled = true;

            //those who are subscribing a message so they are consumers
            //We are saying here that Consumers can receive message asynchronously.
            connectionFactory.DispatchConsumersAsync = true;

            try
            {
                m_rabbitMQConnection = connectionFactory.CreateConnection(Constants.RabbitMQConnectionName);
            }
            catch (Exception ex)
            {

            }
        }

        private void CreateExchange_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to create Exchange due to server not connected", "Exchange Creation", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                channel.ExchangeDeclare(Constants.NotificationExchangeName, ExchangeType.Direct, true, false);
            }
        }

        private void CreateBroadcastingExchange_Click(object sender, RoutedEventArgs e)
        {
            if (!m_bBroadcastingAllowed)
            {
                MessageBox.Show("Broadcasting not allowed", "Broadcasting", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to create Exchange due to server not connected", "Exchange Creation", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                channel.ExchangeDeclare(Constants.BroadcastNotificationExchangeName, ExchangeType.Fanout, true, false);
            }
        }

        private void CreateQueues_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to create Queue due to server not connected", "Queue Creation", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                channel.QueueDeclare(Constants.EmailQueueName, true, false, false);
                channel.QueueDeclare(Constants.SMSQueueName, true, false, false);

                if (m_bBroadcastingAllowed)
                {       //channel.QueueDeclare(Constants.BroadcastEmailQueueName, true, false, false);
                    //channel.QueueDeclare(Constants.BroadcastSMSQueueName, true, false, false);
                    channel.QueueDeclare(Constants.BroadcastQueueName, true, false, false);
                }
            }
        }

        private void BindQueue_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to Bind Queue due to server not connected", "Bind", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                channel.QueueBind(Constants.EmailQueueName, Constants.NotificationExchangeName, Constants.EmailQueueRoutingKey);
                channel.QueueBind(Constants.SMSQueueName, Constants.NotificationExchangeName, Constants.SMSQueueRoutingKey);

                //Broadcast
                if (m_bBroadcastingAllowed)
                    channel.QueueBind(Constants.BroadcastQueueName, Constants.BroadcastNotificationExchangeName, "");
            }
        }

        private void btnPublishEmail_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to publish email due to server not connected", "Publish", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                string stMessage = PublishEmailText.Text;
                var properties = channel.CreateBasicProperties();

                /// Non-persistent (1) or persistent (2).
                properties.DeliveryMode = 2;


                var email = new Email
                {
                    EmailId = "demo@gmail.com",
                    ReceivedFrom = "Demo",
                    Content = stMessage,
                };

                string jsonMessage = JsonConvert.SerializeObject(email);

                // Convert the JSON message to bytes
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                channel.BasicPublish(
                    Constants.NotificationExchangeName,
                    Constants.EmailQueueRoutingKey,
                    properties,
                    body);
            }
        }

        private void btnBroadcastEmail_Click(object sender, RoutedEventArgs e)
        {
            if (!m_bBroadcastingAllowed)
            {
                MessageBox.Show("Broadcasting not allowed", "Broadcasting", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to publish email due to server not connected", "Publish", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                string stMessage = PublishEmailText.Text;
                var properties = channel.CreateBasicProperties();

                /// Non-persistent (1) or persistent (2).
                properties.DeliveryMode = 2;


                var email = new Email
                {
                    EmailId = "demo@gmail.com",
                    ReceivedFrom = "Demo",
                    Content = stMessage,
                };

                string jsonMessage = JsonConvert.SerializeObject(email);

                // Convert the JSON message to bytes
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                channel.BasicPublish(
                    Constants.BroadcastNotificationExchangeName,
                    "",//Constants.BroadcastEmailQueueRoutingKey,
                    properties,
                    body);
            }
        }

        private void btnPublishSms_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to publish sms due to server not connected", "Publish", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                string stMessage = PublishSmsText.Text;
                var properties = channel.CreateBasicProperties();

                /// Non-persistent (1) or persistent (2).
                properties.DeliveryMode = 2;


                var sms = new SMS
                {
                    CellNo = "123-456-7890",
                    ReceivedFrom = "Demo",
                    Content = stMessage,
                };

                string jsonMessage = JsonConvert.SerializeObject(sms);

                // Convert the JSON message to bytes
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                /// Non-persistent (1) or persistent (2).
                properties.DeliveryMode = 2;
                channel.BasicPublish(
                    Constants.NotificationExchangeName,
                    Constants.SMSQueueRoutingKey,
                    properties,
                    body);
            }
        }

        private void btnBroadcastSms_Click(object sender, RoutedEventArgs e)
        {
            if (!m_bBroadcastingAllowed)
            {
                MessageBox.Show("Broadcasting not allowed", "Broadcasting", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Unable to publish sms due to server not connected", "Publish", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var channel = m_rabbitMQConnection.CreateModel())
            {
                string stMessage = PublishSmsText.Text;
                var properties = channel.CreateBasicProperties();

                /// Non-persistent (1) or persistent (2).
                properties.DeliveryMode = 2;


                var sms = new SMS
                {
                    CellNo = "123-456-7890",
                    ReceivedFrom = "Demo",
                    Content = stMessage,
                };

                string jsonMessage = JsonConvert.SerializeObject(sms);

                // Convert the JSON message to bytes
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                /// Non-persistent (1) or persistent (2).
                properties.DeliveryMode = 2;
                channel.BasicPublish(
                    Constants.BroadcastNotificationExchangeName,
                    "",//Constants.BroadcastSMSQueueRoutingKey,
                    properties,
                    body);
            }
        }

        #region Subscribe Queues
        private void EmailQueueSubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Server not connected", "Connection", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            m_emailChannel = m_rabbitMQConnection.CreateModel();
            //Prefetch size = 1
            m_emailChannel.BasicQos(0, 1, false);

            //declare a consumer

            var emailConsumer = new AsyncEventingBasicConsumer(m_emailChannel);
            emailConsumer.Received += EmailConsumer_Received;

            //we set auto acknowledgement as false because we will control acknowledgement of messages
            //consumer is subscribed to Email Queue
            m_emailChannel.BasicConsume(Constants.EmailQueueName, false, emailConsumer);

            #region Subscribe Broadcase Queue

            if (m_bBroadcastingAllowed)
            {
                m_broadcastEmailChannel = m_rabbitMQConnection.CreateModel();
                //Prefetch size = 1
                m_broadcastEmailChannel.BasicQos(0, 1, false);

                //declare a consumer

                var broadcastEmailConsumer = new AsyncEventingBasicConsumer(m_broadcastEmailChannel);
                broadcastEmailConsumer.Received += BroadcastEmailConsumer_Received; ;

                //we set auto acknowledgement as false because we will control acknowledgement of messages
                //consumer is subscribed to Email Queue
                // m_broadcastEmailChannel.BasicConsume(Constants.BroadcastEmailQueueName, false, broadcastEmailConsumer);
                m_broadcastEmailChannel.BasicConsume(Constants.BroadcastQueueName, false, broadcastEmailConsumer);
            }

            #endregion
        }

        private void SMSQueueSubscribe_Click(object sender, RoutedEventArgs e)
        {
            if (m_rabbitMQConnection == null)
            {
                MessageBox.Show("Server not connected", "Connection", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            m_smsChannel = m_rabbitMQConnection.CreateModel();
            m_smsChannel.BasicQos(0, 1, false);

            //declare a consumer

            var smsConsumer = new AsyncEventingBasicConsumer(m_smsChannel);
            smsConsumer.Received += SmsConsumer_Received;

            //we set auto acknowledgement as false because we will control acknowledgement of messages
            //consumer is subscribed to Email Queue
            m_smsChannel.BasicConsume(Constants.SMSQueueName, false, smsConsumer);

            #region Subscribe Broadcase Queue

            if (m_bBroadcastingAllowed)
            {
                m_broadcastsmsChannel = m_rabbitMQConnection.CreateModel();
                m_broadcastsmsChannel.BasicQos(0, 1, false);

                //declare a consumer

                var broadcastSmsConsumer = new AsyncEventingBasicConsumer(m_broadcastsmsChannel);
                broadcastSmsConsumer.Received += BroadcastSmsConsumer_Received;

                //we set auto acknowledgement as false because we will control acknowledgement of messages
                //consumer is subscribed to Email Queue
                m_broadcastsmsChannel.BasicConsume(Constants.BroadcastQueueName, false, broadcastSmsConsumer);
            }

            #endregion
        }

        #endregion

        #region Received from Direct Exchange

        private async Task EmailConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var jsonMessage = Encoding.UTF8.GetString(e.Body.ToArray()); ;
            Email emailReceived = JsonConvert.DeserializeObject<Email>(jsonMessage);

            Dispatcher currentDispatcher = Application.Current.Dispatcher;

            await currentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                LstEmailReceived.Add(emailReceived);
            }));

            //we pass false means we only ack this message not all the pending messages in the queue.
            m_emailChannel.BasicAck(e.DeliveryTag, false);
        }

        private async Task SmsConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            if (e == null)
                return;

            var jsonMessage = Encoding.UTF8.GetString(e.Body.ToArray());
            var sms = JsonConvert.DeserializeObject<SMS>(jsonMessage);

            Dispatcher currentDispatcher = Application.Current.Dispatcher;

            await currentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                LstSmsReceived.Add(sms);
            }));

            //we pass false means we only ack this message not all the pending messages in the queue.
            m_smsChannel.BasicAck(e.DeliveryTag, false);
        }

        #endregion

        #region Received from Fanout Exchange

        private async Task BroadcastEmailConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            if (!m_bBroadcastingAllowed)
            {
                MessageBox.Show("Broadcasting not allowed", "Broadcasting", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var jsonMessage = Encoding.UTF8.GetString(e.Body.ToArray()); ;
            Email emailReceived = JsonConvert.DeserializeObject<Email>(jsonMessage);

            Dispatcher currentDispatcher = Application.Current.Dispatcher;

            await currentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                LstEmailReceived.Add(emailReceived);
            }));

            //we pass false means we only ack this message not all the pending messages in the queue.
            m_emailChannel.BasicAck(e.DeliveryTag, false);
        }

        private async Task BroadcastSmsConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            if (!m_bBroadcastingAllowed)
            {
                MessageBox.Show("Broadcasting not allowed", "Broadcasting", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (e == null)
                return;

            var jsonMessage = Encoding.UTF8.GetString(e.Body.ToArray());
            var sms = JsonConvert.DeserializeObject<SMS>(jsonMessage);

            Dispatcher currentDispatcher = Application.Current.Dispatcher;

            await currentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                LstSmsReceived.Add(sms);
            }));

            //we pass false means we only ack this message not all the pending messages in the queue.
            m_smsChannel.BasicAck(e.DeliveryTag, false);
        }

        #endregion
    }
}
