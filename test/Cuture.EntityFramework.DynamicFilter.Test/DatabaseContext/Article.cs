﻿// <Auto-Generated/>

using System.ComponentModel.DataAnnotations;

namespace Cuture.EntityFramework.DynamicFilter.Test.DatabaseContext;

public class Article : ITenantId, IOrganizationId, ISoftDeletion
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; }

    public string Content { get; set; }

    public bool IsDeleted { get; set; }

    public ulong OrganizationId { get; set; }

    public ulong TenantId { get; set; }

    public override string ToString()
    {
        return $"{Id}【{Title}】TenantId: {TenantId} , OrganizationId: {OrganizationId} , IsDeleted: {IsDeleted}"; 
    }
}
