namespace Project_S.Runtime.Services.Storage
{
    public class PlayerStorage : BasePlayerStorage<PlayerStorage>
    {
        protected override void Init() { }

        public override void Reset()
        {
            DataStorage.Reset();
        }
    }
}