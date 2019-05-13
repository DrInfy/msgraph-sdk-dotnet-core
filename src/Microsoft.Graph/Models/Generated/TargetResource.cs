// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

// **NOTE** This file was generated by a tool and any changes will be overwritten.
// <auto-generated/>

// Template Source: Templates\CSharp\Model\ComplexType.cs.tt

namespace Microsoft.Graph
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// The type TargetResource.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [JsonConverter(typeof(DerivedTypeConverter))]
    public partial class TargetResource
    {

        /// <summary>
        /// Gets or sets id.
        /// Indicates the unique ID of the resource.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id", Required = Newtonsoft.Json.Required.Default)]
        public string Id { get; set; }
    
        /// <summary>
        /// Gets or sets displayName.
        /// Indicates the visible name defined for the resource. Typically specified when the resource is created.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "displayName", Required = Newtonsoft.Json.Required.Default)]
        public string DisplayName { get; set; }
    
        /// <summary>
        /// Gets or sets type.
        /// Describes the resource type.  Example values include Application, Group, ServicePrincipal, and User.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "type", Required = Newtonsoft.Json.Required.Default)]
        public string Type { get; set; }
    
        /// <summary>
        /// Gets or sets userPrincipalName.
        /// When type is set to User, this includes the user name that initiated the action; null for other types.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "userPrincipalName", Required = Newtonsoft.Json.Required.Default)]
        public string UserPrincipalName { get; set; }
    
        /// <summary>
        /// Gets or sets groupType.
        /// When type is set to Group, this indicates the group type.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "groupType", Required = Newtonsoft.Json.Required.Default)]
        public GroupType? GroupType { get; set; }
    
        /// <summary>
        /// Gets or sets modifiedProperties.
        /// Indicates name, old value and new value of each attribute that changed. Property values depend on the operation type.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "modifiedProperties", Required = Newtonsoft.Json.Required.Default)]
        public IEnumerable<ModifiedProperty> ModifiedProperties { get; set; }
    
        /// <summary>
        /// Gets or sets additional data.
        /// </summary>
        [JsonExtensionData(ReadData = true)]
        public IDictionary<string, object> AdditionalData { get; set; }

        /// <summary>
        /// Gets or sets @odata.type.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "@odata.type", Required = Newtonsoft.Json.Required.Default)]
        public string ODataType { get; set; }
    
    }
}
