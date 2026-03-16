namespace Project_S.Runtime.Services.Storage
{
    public class HardcodedBoolValue
    {
        bool _val;

        public HardcodedBoolValue(bool val)
        {
            _val = val;
        }

        public bool Value
        {
            get => _val;

            set
            {

            }
        }
    }
}