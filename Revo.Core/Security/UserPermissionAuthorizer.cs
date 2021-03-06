﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Revo.Core.Security
{
    public class UserPermissionAuthorizer : IUserPermissionAuthorizer
    {
        private readonly IUserContext userContext;
        private readonly IPermissionAuthorizationMatcher permissionAuthorizationMatcher;
        private readonly IUserPermissionResolver userPermissionResolver;
        private readonly IPermissionTypeRegistry permissionTypeRegistry;

        public UserPermissionAuthorizer(IUserContext userContext,
            IPermissionAuthorizationMatcher permissionAuthorizationMatcher,
            IUserPermissionResolver userPermissionResolver,
            IPermissionTypeRegistry permissionTypeRegistry)
        {
            this.userContext = userContext;
            this.permissionAuthorizationMatcher = permissionAuthorizationMatcher;
            this.userPermissionResolver = userPermissionResolver;
            this.permissionTypeRegistry = permissionTypeRegistry;
        }

        public async Task<bool> CheckAuthorizationAsync(IUser user, string permissionId, string resourceId = null, string contextId = null)
        {
            var userPermissions = await userPermissionResolver.GetUserPermissionsAsync(user);
            return permissionAuthorizationMatcher.CheckAuthorization(userPermissions,
                new[] { GetPermission(permissionId, resourceId, contextId) });
        }

        public async Task<bool> CheckCurrentUserAuthorizationAsync(string permissionId, string resourceId = null, string contextId = null)
        {
            var userPermissions = await userContext.GetPermissionsAsync();
            return permissionAuthorizationMatcher.CheckAuthorization(userPermissions,
                new[] { GetPermission(permissionId, resourceId, contextId) });
        }

        private Permission GetPermission(string permissionId, string resourceId, string contextId)
        {
            return new Permission(
                permissionTypeRegistry.GetPermissionTypeById(Guid.Parse(permissionId)),
                resourceId,
                contextId);
        }
    }
}
