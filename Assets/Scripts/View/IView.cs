namespace Gameplay.Core.View
{
    public interface IView
    {
        public ViewType ViewType { get; }
        public void  OnInitialized() {} 
        public void  OnShown() { }
        public void  OnHide() { }
    }
}