using System.Threading.Tasks;

namespace Vertex.Utils.Channels
{
    public class AskInputBox<Input, Output>
    {
        public AskInputBox(Input data)
        {
            this.Value = data;
        }

        public TaskCompletionSource<Output> TaskSource { get; } = new TaskCompletionSource<Output>();

        public Input Value { get; set; }
    }
}
