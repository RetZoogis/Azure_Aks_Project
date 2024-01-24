using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text;

namespace AzureProject
{
    public class AzureService
    {
        private string _cs;
        public AzureService(string cs)
        {
            _cs = cs;
        }
        public void SendQueue(string message, string queuename)
        {
            try
            {
                QueueClient queueClient = new QueueClient(_cs, queuename, new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });

                queueClient.CreateIfNotExists();
                if(queueClient.Exists())
                {
                    queueClient.SendMessageAsync(message);
                    
                }
            }
            catch(Exception e) 
            {
                throw;
            }
        }
        public string SendBlob(string messagecontent, string containername)
        {
            try
            {
                //Notes:
                //We can also initiate BlobClient Directly just like we did in  line 93 
                BlobContainerClient container = new BlobContainerClient(_cs, containername);
               
                var blobname = "couldbe a guid";

                BlobClient blob = container.GetBlobClient(blobname);

                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(messagecontent)))
                {
                    blob.Upload(ms);
                    //blob.DownloadContent();
                }
                return blobname;
            
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void movefrompoisonodstomainqueue(string queuename, string cs)
        {
            QueueClient queueClient = new QueueClient(cs, queuename, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });

            var messages = queueClient.ReceiveMessages(15);
            foreach(var message in messages.Value)
            {
                //send message to other queue
                SendQueue(message.Body.ToString(), queuename);
                //delete message
                queueClient.DeleteMessage(message.MessageId, message.PopReceipt);

            }

        }
        public void sendtoapiwithqueueandblobcontainer(string queuename, string cs)
        {
            QueueClient queueClient = new QueueClient(cs, queuename, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });
            var messages = queueClient.ReceiveMessages(32);
            var list = messages.Value.ToList();
            var sortedmegalist = list.OrderBy(x => x.InsertedOn);
            foreach(var message in sortedmegalist)
            {
                //message.body.tostring  = blobname
                //remember that my message queues have only the blobname (guids)

                BlobClient client =  new BlobClient(cs, "containername", message.Body.ToString());
                

                var payload = client.DownloadContent().Value.Content.ToString();

                //sendtoapi

                //delete queue message
                queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
                //delete blob
                client.Delete();
            }
        }
    }
}
