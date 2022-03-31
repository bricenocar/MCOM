using MCOM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCOM.Business.PostFeedBack.DCF
{
    internal class PostFeedBackDcf
    {
        /*
         * DCF requires the http request to authenticate agaisnt their endpoint. For this, the value of the Authentication header
         * has to be encoded in a specific way. After the header is abse64 encoded the Method sends the new updated object back to the threath.
        */
        /// <summary>
        /// Get queue item based on DCF their business logic
        /// </summary>
        /// <param name="queueItem"></param>
        /// <returns></returns>
        public static QueueItem GetQueueItem(QueueItem queueItem)
        {
            var headers = queueItem.Headers;

            // The Authentication header has to be encoded
            if (headers.TryGetValue("Authentication", out var authHeader))
            {
                var authHeaderBytes = Encoding.UTF8.GetBytes(authHeader);
                var base64Header = Convert.ToBase64String(authHeaderBytes);
                var newAuthHeader = $"Basic {base64Header}";

                queueItem.Headers["Authentication"] = newAuthHeader;
            }

            return queueItem;
        }
    }
}
