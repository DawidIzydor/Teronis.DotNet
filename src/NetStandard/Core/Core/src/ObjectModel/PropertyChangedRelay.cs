﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Teronis.Reflection;
using Teronis.Reflection.Caching;
using Teronis.Utils;

namespace Teronis.ObjectModel
{
    public class PropertyChangedRelay : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? NotifiersPropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
            add => NotifiersPropertyChanged += value;
            remove => NotifiersPropertyChanged -= value;
        }

        public Dictionary<string, Type>? AllowedProperties { get; set; }
        public readonly ReadOnlyCollection<INotifyPropertyChanged> PropertyChangedNotifiers;

        private readonly BindingFlags propertyBindingFlags;
        private readonly List<INotifyPropertyChanged> propertyChangedNotifiers;

        public PropertyChangedRelay()
        {
            propertyBindingFlags = VariableInfoDescriptor.DefaultFlags | BindingFlags.GetProperty;
            propertyChangedNotifiers = new List<INotifyPropertyChanged>();
            PropertyChangedNotifiers = new ReadOnlyCollection<INotifyPropertyChanged>(propertyChangedNotifiers);
        }

        ///// <summary>
        ///// The notifying changes will be coming from <paramref name="propertyChangedNotifiers"/>, 
        ///// but restricted by <see cref="AllowedProperties"/>.
        ///// </summary>
        //public PropertyChangedRelay(IEnumerable<KeyValuePair<string, Type>> allowedProperties, params INotifyPropertyChanged[] propertyChangedNotifiers)
        //{
        //    onConstruction();
        //    AddAllowedProperties(allowedProperties);
        //    SubscribePropertyChangedNotifiers(propertyChangedNotifiers);
        //}

        ///// <summary>
        ///// The notifying changes will be restricted by 
        ///// </summary>
        ///// <param name="allowedProperties"></param>
        //public PropertyChangedRelay(IEnumerable<KeyValuePair<string, Type>> allowedProperties)
        //{
        //    onConstruction();
        //    AddAllowedProperties(allowedProperties);
        //}

        ///// <summary>
        ///// The notifying changes are from notifiable containers of <paramref name="propertyChangedNotifiers"/>.
        ///// </summary>
        ///// <param name="commonPropertiesContainerType">Only the members of an instance of <see cref="CommonPropertiesContainerType"/> are going to relay</param>
        ///// <param name="propertyChangedNotifiers">In most scenarios, the property changed notifiers (instances of <see cref="INotifyPropertyChanged"/>) are children of an instance of <see cref="CommonPropertiesContainerType"/>.</param>
        //public PropertyChangedRelay(params INotifyPropertyChanged[] propertyChangedNotifiers)
        //{
        //    onConstruction();
        //    SubscribePropertyChangedNotifiers(propertyChangedNotifiers);
        //}

        ///// <summary>
        ///// Relay upcoming property changes that are in common with members of a type of the instance of <see cref="commonPropertiesContainerType"/>.
        ///// The notifying changes are from notifiable containers of <paramref name="propertyChangedNotifiers"/>.
        ///// </summary>
        ///// <param name="commonPropertiesContainerType">Only the members of an instance of <see cref="CommonPropertiesContainerType"/> are going to relay</param>
        ///// <param name="propertyChangedNotifiers">In most scenarios, the property changed notifiers (instances of <see cref="INotifyPropertyChanged"/>) are children of an instance of <see cref="CommonPropertiesContainerType"/>.</param>
        //public PropertyChangedRelay(Type commonPropertiesContainerType, params INotifyPropertyChanged[] propertyChangedNotifiers)
        //{
        //    onConstruction();
        //    var propertyInfos = commonPropertiesContainerType.GetProperties(propertyBindingFlags);
        //    AddAllowedProperties(propertyInfos);
        //    SubscribePropertyChangedNotifiers(propertyChangedNotifiers);
        //}



        ///// <summary>
        ///// Notify about property changes from notifiable members of the instance of <see cref="commonPropertiesContainerType"/>.
        ///// Notifiable members are automatically un-/subscribing.
        ///// </summary>
        //public PropertyChangedRelay(INotifyPropertyChanged commonPropertiesContainerNotifier)
        //    : this(commonPropertiesContainerNotifier?.GetType())
        //{
        //    propertyChangedNotifiersCache = new SingleTypePropertyCache<INotifyPropertyChanged>(commonPropertiesContainerNotifier);

        //    void PropertyChangedNotifiersCache_PropertyCacheAdded(object sender, PropertyCachedEventArgs<INotifyPropertyChanged> args)
        //        => SubscribePropertyChangedNotifier(args.AddedPropertyValue);

        //    propertyChangedNotifiersCache.PropertyAdded += PropertyChangedNotifiersCache_PropertyCacheAdded;

        //    void PropertyChangedNotifiersCache_PropertyCacheRemoved(object sender, PropertyCacheRemovedEventArgs<INotifyPropertyChanged> args)
        //        => UnsubscribePropertyChangedNotifier(args.OldPropertyValue);

        //    propertyChangedNotifiersCache.PropertyRemoved += PropertyChangedNotifiersCache_PropertyCacheRemoved;
        //}

        private void ensureAllowedPropertiesInitialization()
        {
            if (AllowedProperties != null) {
                return;
            }

            AllowedProperties ??= new Dictionary<string, Type>();
        }

        public PropertyChangedRelay AddAllowedProperty(string propertyName, Type valueType)
        {
            ensureAllowedPropertiesInitialization();
            AllowedProperties!.Add(propertyName, valueType);
            return this;
        }

        public PropertyChangedRelay AddAllowedProperty<T>(Expression<Func<T, object?>> expression)
        {
            var returnName = ExpressionUtils.GetReturnName(expression);
            var returnType = ExpressionUtils.GetReturnType(expression);
            return AddAllowedProperty(returnName, returnType);
        }

        public PropertyChangedRelay AddAllowedProperties(IEnumerable<KeyValuePair<string, Type>> allowedProperties)
        {
            ensureAllowedPropertiesInitialization();
            var allowedPropertyCollection = (ICollection<KeyValuePair<string, Type>>)AllowedProperties!;

            foreach (var allowedProperty in allowedProperties) {
                allowedPropertyCollection.Add(allowedProperty);
            }

            return this;
        }

        public PropertyChangedRelay AddAllowedProperties(IEnumerable<PropertyInfo> propertyInfos)
        {
            ensureAllowedPropertiesInitialization();

            foreach (var propertyInfo in propertyInfos) {
                AllowedProperties!.Add(propertyInfo.Name, propertyInfo.PropertyType);
            }

            return this;
        }

        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            => NotifiersPropertyChanged?.Invoke(sender, e);

        private void PropertyChangedNotifier_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var shouldNotifyUnknownProperty = AllowedProperties == null
                || AllowedProperties.TryGetValue(e.PropertyName, out var propertyType)
                    && (propertyType == null
                        || sender.GetType().GetProperty(e.PropertyName, propertyBindingFlags)?.PropertyType == propertyType);

            if (!shouldNotifyUnknownProperty) {
                return;
            }

            OnPropertyChanged(sender, e);
        }

        public PropertyChangedRelay SubscribePropertyChangedNotifier(INotifyPropertyChanged propertyChangedNotifier)
        {
            propertyChangedNotifier = propertyChangedNotifier ??
                throw new ArgumentNullException(nameof(propertyChangedNotifier));

            if (!propertyChangedNotifiers.Contains(propertyChangedNotifier)) {
                propertyChangedNotifier.PropertyChanged += PropertyChangedNotifier_PropertyChanged;
                propertyChangedNotifiers.Add(propertyChangedNotifier);
            }

            return this;
        }

        public PropertyChangedRelay SubscribePropertyChangedNotifiers(params INotifyPropertyChanged[] propertyChangedNotifiers)
        {
            if (propertyChangedNotifiers != null) {
                foreach (var propertyChangedNotifier in propertyChangedNotifiers) {
                    SubscribePropertyChangedNotifier(propertyChangedNotifier);
                }
            }

            return this;
        }

        private void Cache_PropertyAdded(object sender, PropertyCachedEventArgs<INotifyPropertyChanged> args) =>
            SubscribePropertyChangedNotifier(args.PropertyValue!);

        private void Cache_PropertyRemoved(object sender, PropertyCacheRemovedEventArgs<INotifyPropertyChanged> args) =>
            UnsubscribePropertyChangedNotifier(args.PropertyValue!);

        public PropertyChangedRelaySubscription SubscribeSingleTypePropertyCache(SingleTypePropertyCache<INotifyPropertyChanged> cache)
        {
            cache.PropertyAdded += Cache_PropertyAdded;
            cache.PropertyRemoved += Cache_PropertyRemoved;
            return new PropertyChangedRelaySubscription(this, cache);
        }

        public void UnsubscribePropertyChangedNotifier(INotifyPropertyChanged propertyChangedNotifier)
        {
            propertyChangedNotifier = propertyChangedNotifier ??
                throw new ArgumentNullException(nameof(propertyChangedNotifier));

            if (!propertyChangedNotifiers.Contains(propertyChangedNotifier)) {
                return;
            }

            propertyChangedNotifier.PropertyChanged -= PropertyChangedNotifier_PropertyChanged;
            propertyChangedNotifiers.Remove(propertyChangedNotifier);
        }
    }
}
