using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Livet.EventListeners;

namespace MetroTrilithon.Mvvm;

public static class PropertyChangedExtensions
{
    public static IDisposable Subscribe<TTarget, TProperty>(
        this TTarget source,
        Expression<Func<TTarget, TProperty>> propertyExpression,
        Action<TProperty> action,
        bool immediately = true)
        where TTarget : INotifyPropertyChanged
    {
        var getter = PropertyAccessor<TTarget>.Getter(propertyExpression, out var propertyName);
        if (immediately) action(getter(source));

        return new PropertyChangedEventListener(source)
        {
            { propertyName, (_, _) => action(getter(source)) },
        };
    }

    private static class PropertyAccessor<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Dictionary<string, Delegate> _getters = new();

        public static Func<T, TProperty> Getter<TProperty>(Expression<Func<T, TProperty>> propertyExpression, out string propertyName)
        {
            propertyName = ((MemberExpression)propertyExpression.Body).Member.Name;
            Delegate? getter;

            lock (_getters)
            {
                if (_getters.TryGetValue(propertyName, out getter) == false)
                {
                    getter = propertyExpression.Compile();
                    _getters.Add(propertyName, getter);
                }
            }

            return (Func<T, TProperty>)getter;
        }
    }
}
