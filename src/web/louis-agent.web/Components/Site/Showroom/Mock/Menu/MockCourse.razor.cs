using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Menu;

/// <summary>A course of a mocked menu (F127.1): its name over its dishes.</summary>
public partial class MockCourse
{
    /// <summary>The course's name: "Starters".</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>Its dishes.</summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;
}
