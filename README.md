# DynamicStyle #

Responsive layout for Universal Apps

The DynamicStyle project has two functions.

1. Automatically set the VisualState based on the size, shape, orientation of the screen
2. Automatically set element Styles based on the size, shape, orientation of the screen

### Why? ###
**Control Styles** are also awesome. You can set things like foreground color, font sizes, margins and save them in a style to reuse.

**Visual States** are awesome.  They allow you to make different layouts and styles based on named states.
Maybe you have two basic states for your phone project.  Landscape and Portrait (apparently this is old terminology and we should be saying Wide and Tall)
Or maybe you have 5 different states.  XWide, Wide, Default, Tall, Skinny

**But**, in XWide Visual State you want to increase the font weight to be X, for Tall Visual State you want the font size to decrease.  When you have lots of controls on a screen with different visual states, the awesomeness of Visual States is broken because you have to change the properties for all of the controls for each of the states.  It also breaks control style because you have the values scattered between Style definitions and Visual States


## Defining Control Styles ##
OK, so you have defined an awesome style for your titles.

``` xml
<Style x:Key="Title" TargetType="TextBlock">
    <Setter Property="FontSize" Value="32"/>
</Style>
```

But when the screen is XWide, you want the font to be a bit bigger.
``` xml
<Style x:Key="Title.XWide" TargetType="TextBlock">
    <Setter Property="FontSize" Value="46"/>
</Style>
```

And when the app is snapped to the left, you want the font a little smaller.
``` xml
<Style x:Key="Title.Tall" TargetType="TextBlock">
    <Setter Property="FontSize" Value="24"/>
</Style>
```

Styles are soo awesome. You can set different margins, colors, content, .. any property you like, and store them in a named style.
Using the above convention, you can store styles using the state name as a suffix (Title.XWide, Title.Tall)

As Styles are typically defined globally (a very good practice), the definitions of the state properties are also stored globally.  The following is the default StyleRules, but you can override these with whatever you like. 

``` csharp
Dynamic.StyleRules = new List<IDynamicRule>
{
    new ShapeRule { Name = "XWide", IsWide = true, MinWidth = 1366 },
    new ShapeRule { Name = "Wide", IsWide = true },
    new ShapeRule { Name = "Tall", IsTall = true }
};
```

The normal approach to setting a control style would be like this:
``` xml
<TextBlock Text="{Binding Title}" Style="{StaticResource Title}"/>
``` 

The DynamicStyle approach would look like this:
``` xml
<TextBlock Text="{Binding Title}" dynamic:Dynamic.Style="Title"/>
``` 

According to the rules above, when the screen is wider than 1366, the control will have the `Title.XWide` style applied.  When the app is snapped to the left, the control will have the `Title.Tall` style applied.  If it doesn't meet those two criteria, then the `Title` style is applied.



## VisualState ##
Maybe you have two basic visual states called Wide and Tall.  When you hold the screen in portrait, you want the Tall Visual state applied, and when in landscape, you want the Wide style applied.
In the past you could monitor the orientation change, check the size and set the state.  Not a lot of code, but now you can define the rules as part of your XAML.

``` xml
<dynamic:Dynamic.VisualStateRules>
    <dynamic:ShapeRule Name="Wide" IsWide="True"/>
    <dynamic:ShapeRule Name="Tall" IsTall="True"/>
</dynamic:Dynamic.VisualStateRules>
```

Maybe your Visual State is a little more fine grained.  Defining the rules for when to switch state is a lot quicker than code.
``` xml
<dynamic:Dynamic.VisualStateRules>
    <dynamic:ShapeRule Name="XWide" IsWide="True" MinWidth="1366"/>
    <dynamic:ShapeRule Name="Wide" IsWide="True"/>
    <dynamic:ShapeRule Name="Skinny" IsTall="True" MaxWidth="320"/>
    <dynamic:ShapeRule Name="Tall" IsTall="True"/>
    <dynamic:ShapeRule Name="Default" />
</dynamic:Dynamic.VisualStateRules>
```

Rules are evaluates top to bottom and the first that matches get applied.  

Note: the last rule `Default` has no parameters, so will always be used if the other rules are not set.

