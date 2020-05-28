﻿using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ZNetCS.AspNetCore.Authentication.Basic.Events;
using Teronis.Identity.Authentication.Extensions;
using Teronis.Identity.Authentication.Tools;
using Teronis.Identity.Entities;
using ZNetCS.AspNetCore.Authentication.Basic;

namespace Teronis.Identity.Authentication
{
    public static class AuthenticationBuilderExtensions
    {
        private static AuthenticationBuilder addIdentityBasic(AuthenticationBuilder authenticationBuilder, Action<Action<BasicAuthenticationOptions>> addBasicAuthentication, Action<BasicAuthenticationOptions>? configureOptions = null)
        {
            addBasicAuthentication(options => {
                configureOptions?.Invoke(options);

                options.Events = (options.Events ?? new BasicAuthenticationEvents())
                   .UseAuthenticateWhenValidatePrincipal();
            });

            return authenticationBuilder;
        }

        /// <summary>
        /// Uses <see cref="AuthenticationDefaults.IdentityBasicScheme"/> as scheme.
        /// </summary>
        public static AuthenticationBuilder AddIdentityBasic(this AuthenticationBuilder authenticationBuilder, Action<BasicAuthenticationOptions>? configureOptions = null)
        {
            return addIdentityBasic(authenticationBuilder, _configureOptions =>
                authenticationBuilder.AddBasicAuthentication(AuthenticationDefaults.IdentityBasicScheme, _configureOptions), configureOptions);
        }

        /// <summary>
        /// Uses <see cref="AuthenticationDefaults.IdentityBasicScheme"/> as scheme.
        /// </summary>
        public static AuthenticationBuilder AddIdentityBasic(this AuthenticationBuilder authenticationBuilder, string authenticationScheme, Action<BasicAuthenticationOptions>? configureOptions)
        {
            return addIdentityBasic(authenticationBuilder, _configureOptions =>
                authenticationBuilder.AddBasicAuthentication(authenticationScheme, _configureOptions), configureOptions);
        }

        private static void validateJwtBearerAuthenticationOptions(JwtBearerAuthenticationOptions options)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));
            Validator.ValidateObject(options, new ValidationContext(options), true);
        }

        private static AuthenticationBuilder addIdentityJwtRefreshToken<BearerTokenType>(AuthenticationBuilder authenticationBuilder, Action<Action<JwtBearerOptions>> addJwtBearer, JwtBearerAuthenticationOptions options)
            where BearerTokenType : class, IBearerTokenEntity
        {
            validateJwtBearerAuthenticationOptions(options);

            addJwtBearer(jwtBearerOptions => {
                jwtBearerOptions.RequireHttpsMetadata = false;

                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = options.TokenSigningKey,
                    /// Is mandatory for <see cref="TokenValidatedContextTools.ValidateRefreshTokenIdClaim"/>.
                    SaveSigninToken = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };

                jwtBearerOptions.Events = new JwtBearerEvents()
                   .WhenTokenValidated(
                       // The order matters! When validating, the user
                       // related identity is added to the claims principal.
                       TokenValidatedContextTools.ValidateRefreshTokenIdClaim<BearerTokenType>,
                       TokenValidatedContextTools.ValidateSecurityStamp);
            });

            return authenticationBuilder;
        }

        public static AuthenticationBuilder AddIdentityJwtRefreshToken<BearerTokenType>(this AuthenticationBuilder authenticationBuilder, string authenticationScheme, JwtBearerAuthenticationOptions options)
            where BearerTokenType : class, IBearerTokenEntity
        {
            authenticationScheme = authenticationScheme ?? throw new ArgumentNullException(nameof(authenticationScheme));

            return addIdentityJwtRefreshToken<BearerTokenType>(authenticationBuilder,
                configureOptions => authenticationBuilder.AddJwtBearer(authenticationScheme, configureOptions),
                options);
        }

        public static AuthenticationBuilder AddIdentityJwtRefreshToken<BearerTokenType>(this AuthenticationBuilder authenticationBuilder, JwtBearerAuthenticationOptions options)
            where BearerTokenType : class, IBearerTokenEntity
        {
            return addIdentityJwtRefreshToken<BearerTokenType>(authenticationBuilder,
                configureOptions => authenticationBuilder.AddJwtBearer(AuthenticationDefaults.IdentityRefreshTokenBearerScheme, configureOptions),
                options);
        }



        public static AuthenticationBuilder AddIdentityJwtRefreshToken(this AuthenticationBuilder authenticationBuilder, string authenticationScheme, JwtBearerAuthenticationOptions options) =>
            AddIdentityJwtRefreshToken<BearerTokenEntity>(authenticationBuilder, authenticationScheme, options);

        public static AuthenticationBuilder AddIdentityJwtRefreshToken(this AuthenticationBuilder authenticationBuilder, JwtBearerAuthenticationOptions options) =>
            AddIdentityJwtRefreshToken<BearerTokenEntity>(authenticationBuilder, options);

        private static AuthenticationBuilder addIdentityJwtAccessToken(AuthenticationBuilder authenticationBuilder, Action<Action<JwtBearerOptions>> addJwtBearer, JwtBearerAuthenticationOptions options)
        {
            validateJwtBearerAuthenticationOptions(options);

            addJwtBearer(jwtBearerOptions => {
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters() {
                    IssuerSigningKey = options.TokenSigningKey,
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = false,
                    ValidateIssuer = false
                };
            });

            return authenticationBuilder;
        }

        public static AuthenticationBuilder AddJwtAccessToken(this AuthenticationBuilder authenticationBuilder, string authenticationScheme, JwtBearerAuthenticationOptions options)
        {
            authenticationScheme = authenticationScheme ?? throw new ArgumentNullException(nameof(authenticationScheme));

            return addIdentityJwtAccessToken(authenticationBuilder,
                configureOptions => authenticationBuilder.AddJwtBearer(authenticationScheme, configureOptions),
                options);
        }

        public static AuthenticationBuilder AddJwtAccessToken(this AuthenticationBuilder authenticationBuilder, JwtBearerAuthenticationOptions options)
        {
            return addIdentityJwtAccessToken(authenticationBuilder,
                configureOptions => authenticationBuilder.AddJwtBearer(AuthenticationDefaults.AccessTokenBearerScheme, configureOptions),
                options);
        }
    }
}
