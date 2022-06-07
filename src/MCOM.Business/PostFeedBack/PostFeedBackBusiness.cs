using MCOM.Models.Azure;

namespace MCOM.Business.PostFeedBack
{
    public interface IPostFeedBackBusiness
    {
        QueueItem GetQueueItem(QueueItem queueItem);
    }

    public class PostFeedBackBusiness : IPostFeedBackBusiness
    {
        public virtual QueueItem GetQueueItem(QueueItem queueItem)
        {
            var source = queueItem.Source;

            switch (source.ToLower())
            {
                case "dcf":
                    queueItem = DCF.PostFeedBackDcf.GetQueueItem(queueItem);
                    break;

                default:
                    // Do nothing, return same queue item
                    break;
            }

            return queueItem;
        }
    }
}
