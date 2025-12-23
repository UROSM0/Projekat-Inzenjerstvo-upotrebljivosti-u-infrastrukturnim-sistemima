using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using NetworkService.Helpers;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public enum IdCompareOp { None, Less, Greater, Equal }
    public class EntitiesViewModel : BindableBase
    {
        public ObservableCollection<ReactorTemp> Entities { get; }
        public ObservableCollection<SensorType> SensorTypes { get; }

        private ReactorTemp _current = new ReactorTemp();
        public ReactorTemp Current
        {
            get => _current;
            set { SetProperty(ref _current, value); }
        }

        private ReactorTemp _selectedEntity;
        public ReactorTemp SelectedEntity
        {
            get => _selectedEntity;
            set { SetProperty(ref _selectedEntity, value); DeleteCommand.RaiseCanExecuteChanged(); }
        }

        public MyICommand AddCommand { get; }
        public MyICommand DeleteCommand { get; }

        private SensorType _selectedTypeFilter;              
        public SensorType SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set { SetProperty(ref _selectedTypeFilter, value); _view?.Refresh(); }
        }

        private IdCompareOp _idCompare = IdCompareOp.None;   
        public IdCompareOp IdCompare
        {
            get => _idCompare;
            set { SetProperty(ref _idCompare, value); _view?.Refresh(); }
        }

        private string _idFilterText;                        
        public string IdFilterText
        {
            get => _idFilterText;
            set { SetProperty(ref _idFilterText, value); _view?.Refresh(); }
        }

        
        private readonly ICollectionView _view;
        public ICollectionView FilteredEntities => _view;

        
        public MyICommand ClearFilterCommand { get; }

        public EntitiesViewModel(ObservableCollection<ReactorTemp> entities,
                                 ObservableCollection<SensorType> sensorTypes)
        {
            Entities = entities;
            SensorTypes = sensorTypes;

            Current.Type = SensorTypes.FirstOrDefault();
            _view = CollectionViewSource.GetDefaultView(Entities);
            _view.Filter = FilterPredicate;

            AddCommand = new MyICommand(OnAdd);
            DeleteCommand = new MyICommand(OnDelete, CanDelete);
            ClearFilterCommand = new MyICommand(ClearFilter);
        }

        private void OnAdd()
        {
            Current.Validate();

            if (Current.ValidationErrors.IsValid)
            {
                if (Current.Id == null || Entities.Any(e => e.Id == Current.Id))
                {
                    Current.ValidationErrors[nameof(ReactorTemp.Id)] =
                        (Current.Id == null) ? "ID is required." : "ID must be unique.";
                }
            }

           
            if (!Current.ValidationErrors.IsValid) return;


            Entities.Add(new ReactorTemp
            {
                Id = Current.Id.Value,
                Name = Current.Name,
                Type = Current.Type,
                LastValue = null
            });

            Current = new ReactorTemp { Type = SensorTypes.FirstOrDefault() };
        }



        private void OnDelete()
        {
            if (SelectedEntity == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete entity {SelectedEntity.Name} (ID={SelectedEntity.Id})?",
                "Confirm Delete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Entities.Remove(SelectedEntity);
            }
        }

        private bool CanDelete()
        {
            return SelectedEntity != null;
        }

        private bool FilterPredicate(object obj)
        {
            var e = obj as ReactorTemp;
            if (e == null) return false;


            if (SelectedTypeFilter != null && e.Type != SelectedTypeFilter)
                return false;


            if (IdCompare != IdCompareOp.None && int.TryParse(IdFilterText, out int idValue))
            {
                switch (IdCompare)
                {
                    case IdCompareOp.Less: if (!(e.Id < idValue)) return false; break;
                    case IdCompareOp.Greater: if (!(e.Id > idValue)) return false; break;
                    case IdCompareOp.Equal: if (!(e.Id == idValue)) return false; break;
                }
            }

            return true;
        }

        private void ClearFilter()
        {
            SelectedTypeFilter = null;
            IdCompare = IdCompareOp.None;
            IdFilterText = string.Empty;
            _view.Refresh();
        }
    }



}
