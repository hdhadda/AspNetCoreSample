namespace TestWebApi.Interfaces
{
    using System.Collections.Generic;
    using TestWebApi.Models;

    public interface IToDoItemsList
    {
        bool DoesItemExist(string id);
        IEnumerable<ToDoItem> All { get; }
        ToDoItem Find(string id);
        void Insert(ToDoItem item);
        void Update(ToDoItem item);
        void Delete(string id);
    }
}
