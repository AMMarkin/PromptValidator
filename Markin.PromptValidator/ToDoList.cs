using System.ComponentModel;

namespace Markin.PromptValidator;

[Description("ToDo-лист")]
public class ToDoList
{
    [Description("Текст задачи")]
    public required string[] Tasks { get; set; }
}