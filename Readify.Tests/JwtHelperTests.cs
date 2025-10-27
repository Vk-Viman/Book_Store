using Microsoft.Extensions.Configuration;
using Readify.Helpers;
using Readify.Models;
using System.Collections.Generic;
using Xunit;

public class JwtHelperTests
{
    [Fact]
    public void GenerateToken_ReturnsString()
    {
        var inMemorySettings = new Dictionary<string, string> {
            { "Jwt:Key", "test-secret-key-which-is-long-enough" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" }
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var helper = new JwtHelper(configuration);

        var user = new User { Id = 1, Email = "u@example.com", Role = "User" };
        var token = helper.GenerateToken(user);
        Assert.False(string.IsNullOrEmpty(token));
    }
}
