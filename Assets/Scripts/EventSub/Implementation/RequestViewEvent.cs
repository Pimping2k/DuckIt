using Gameplay.Core.View;
using View;

namespace EventSub.Implementation
{
    public struct RequestViewEvent : IEvent
    {
        public ViewType ViewType;
        public object[] Payload;
        
        public RequestViewEvent(ViewType viewType,params object[] payload)
        {
            Payload = new object[]{};
            ViewType = viewType;   
            Payload = payload;
        }
    }
}