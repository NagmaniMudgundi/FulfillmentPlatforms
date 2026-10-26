using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Common.Messaging
{
    public interface IEventPublisher
    {//What’s <T>? A generic method: “I can publish any type of message.” Today OrderStatusChangedEvent, tomorrow maybe OrderPlacedEvent, with the same publisher. You’ve used generics a lot (List<T>, DbSet<T>), now you write one.
        /// <summary>
        /// Publishes an event of type T.
        /// </summary>
        /// <typeparam name="T">The type of the event to publish.</typeparam>
        /// <param name="message">The event message to publish.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync<T>(T message);
    }
}
