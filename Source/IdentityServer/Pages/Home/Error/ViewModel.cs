// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace IdentityServer.Pages.Home.Error;
using Duende.IdentityServer.Models;

public class ViewModel
{
    public ViewModel()
    {
    }

    public ViewModel(string error) => this.Error = new ErrorMessage { Error = error };

    public ErrorMessage Error { get; set; }
}