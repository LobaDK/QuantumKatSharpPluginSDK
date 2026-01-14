using System.ComponentModel.DataAnnotations;

namespace Tests.TestPlugins.EntityFrameworkTestPlugin.Models;

public class TestEntityModel
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
}
