using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace QBTracker.Util
{
    public class ValidatableModel : INotifyDataErrorInfo, INotifyPropertyChanged
    {
        private readonly ConcurrentDictionary<string, List<string>> errors = new();

        private readonly ConcurrentDictionary<string, byte> touchedProperties = new ConcurrentDictionary<string, byte>();

        public virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = "")
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName != nameof(HasErrors))
            {
                touchedProperties.TryAdd(propertyName, 1);
                ValidateAsync();
            }
        }

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public event PropertyChangedEventHandler?              PropertyChanged;

        protected void AddError(string key, string message)
        {
            var errorList = errors.GetOrAdd(key, s => new List<string>());
            errorList.Add(message);
            OnErrorsChanged(key);
        }

        protected void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        public virtual IEnumerable GetErrors(string? propertyName)
        {
           propertyName ??= "";
            errors.TryGetValue(propertyName, out var errorsForName);
            return errorsForName ?? [];
        }

        public virtual bool HasErrors
        {
            get
            {
                return errors.Count > 0 && errors.Any(kv => kv.Value != null && kv.Value.Count > 0);
            }
        }

        public bool WasTouched => touchedProperties.Count > 0;

        public void ClearTouched()
        {
            touchedProperties.Clear();
        }

        public Task ValidateAsync()
        {
            return Task.Run(() => ValidateCore(true));
        }

        private readonly Lock _lock = new();
        public virtual void Validate()
        {
            ValidateCore(false);
        }

        public void ClearErrors()
        {
            foreach (var kv in errors.ToList())
            {
                errors.TryRemove(kv.Key, out _);
                OnErrorsChanged(kv.Key);
            }
        }

        private void ValidateCore(bool touched)
        {
            lock (_lock)
            {
                var validationContext = new ValidationContext(this);
                var validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(this, validationContext, validationResults, true);

                foreach (var kv in errors.ToList())
                {
                    if (validationResults.All(r => r.MemberNames.All(m => m != kv.Key)))
                    {
                        errors.TryRemove(kv.Key, out _);
                        OnErrorsChanged(kv.Key);
                    }
                }

                var q = from r in validationResults
                        from m in r.MemberNames
                        group r by m into g
                        where !touched || touchedProperties.ContainsKey(g.Key)
                        select g;

                foreach (var prop in q)
                {
                    var messages = prop.Select(r => r.ErrorMessage).ToList();

                    if (errors.ContainsKey(prop.Key))
                    {
                        errors.TryRemove(prop.Key, out _);
                    }
                    errors.TryAdd(prop.Key, messages);
                    touchedProperties.TryAdd(prop.Key, 1);
                    OnErrorsChanged(prop.Key);
                }
                NotifyOfPropertyChange(nameof(HasErrors));
            }
        }
    }
}
