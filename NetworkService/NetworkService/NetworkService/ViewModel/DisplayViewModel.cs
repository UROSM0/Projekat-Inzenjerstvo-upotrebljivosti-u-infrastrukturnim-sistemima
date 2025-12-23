using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using NetworkService.Helpers;
using NetworkService.Model;

namespace NetworkService.ViewModel
{
    public class DisplayViewModel : BindableBase
    {
        public ObservableCollection<ReactorTemp> Entities { get; }
        public ObservableCollection<SensorType> SensorTypes { get; }

        public ObservableCollection<SlotVM> Slots { get; } = new ObservableCollection<SlotVM>();

        private ListCollectionView _unplacedView;
        public ICollectionView UnplacedEntities => _unplacedView;

        private readonly HashSet<int> _placedIds = new HashSet<int>();

        
        public ObservableCollection<ConnectionVM> Connections { get; } = new ObservableCollection<ConnectionVM>();

        private bool _isConnectMode;
        public bool IsConnectMode
        {
            get => _isConnectMode;
            set { SetProperty(ref _isConnectMode, value); if (!value) SelectedForConnect = null; }
        }

        private SlotVM _selectedForConnect;
        public SlotVM SelectedForConnect
        {
            get => _selectedForConnect;
            set { SetProperty(ref _selectedForConnect, value); }
        }

        
        public MyICommand<object> DropToSlotCommand { get; }
        public MyICommand<SlotVM> ClearSlotCommand { get; }
        public MyICommand ToggleConnectModeCommand { get; }
        public MyICommand<SlotVM> SlotClickCommand { get; }

        public DisplayViewModel(ObservableCollection<ReactorTemp> entities,
                                ObservableCollection<SensorType> sensorTypes)
        {
            Entities = entities;
            SensorTypes = sensorTypes;

            for (int i = 0; i < 12; i++)
                Slots.Add(new SlotVM(i, this));

            _unplacedView = new ListCollectionView(Entities);
            _unplacedView.Filter = o => IsUnplaced((ReactorTemp)o);
            _unplacedView.GroupDescriptions.Add(new PropertyGroupDescription("Type.Name"));

            DropToSlotCommand = new MyICommand<object>(OnDropToSlot);
            ClearSlotCommand = new MyICommand<SlotVM>(slot => RemoveFromSlot(slot.Index));
            ToggleConnectModeCommand = new MyICommand(() => IsConnectMode = !IsConnectMode);
            SlotClickCommand = new MyICommand<SlotVM>(OnSlotClicked);
        }

        internal bool IsUnplaced(ReactorTemp e)
        {
            if (e == null || !e.Id.HasValue) return false;
            return !_placedIds.Contains(e.Id.Value);
        }

        private void OnDropToSlot(object payloadObj)
        {
            var payload = payloadObj as DropPayload;
            var entity = payload?.Data as ReactorTemp;
            var slot = payload?.Param as SlotVM;
            if (entity == null || slot == null) return;
            PlaceInSlot(slot.Index, entity);
        }

        internal void PlaceInSlot(int slotIndex, ReactorTemp e)
        {
            if (e?.Id == null) return;

           
            var prev = Slots.FirstOrDefault(s => s.Occupant?.Id == e.Id);
            if (prev != null && prev.Index != slotIndex)
            {
                prev.Occupant = null;
               
                foreach (var c in Connections)
                {
                    if (c.A == prev) c.A = Slots[slotIndex];
                    if (c.B == prev) c.B = Slots[slotIndex];
                }
            }

          
            var existing = Slots[slotIndex].Occupant;
            if (existing?.Id != null && existing.Id != e.Id)
            {
                RemoveConnectionsOfSlot(Slots[slotIndex]);
                _placedIds.Remove(existing.Id.Value);
            }

            
            Slots[slotIndex].Occupant = e;
            _placedIds.Add(e.Id.Value);

            _unplacedView.Refresh();
        }

        internal void RemoveFromSlot(int slotIndex)
        {
            var e = Slots[slotIndex].Occupant;
            if (e?.Id != null)
            {
                _placedIds.Remove(e.Id.Value);
                
                RemoveConnectionsOfSlot(Slots[slotIndex]);
                Slots[slotIndex].Occupant = null;
                _unplacedView.Refresh();
            }
        }

        private void RemoveConnectionsOfSlot(SlotVM slot)
        {
            var toRemove = Connections.Where(c => c.A == slot || c.B == slot).ToList();
            foreach (var c in toRemove) Connections.Remove(c);
        }

        private void OnSlotClicked(SlotVM slot)
        {
            if (!IsConnectMode) return;
            if (slot?.Occupant == null) return; 

            if (SelectedForConnect == null)
            {
                SelectedForConnect = slot; 
            }
            else
            {
                if (SelectedForConnect != slot)
                {
                    TryAddConnection(SelectedForConnect, slot);
                }
                SelectedForConnect = null; 
            }
        }

        private void TryAddConnection(SlotVM a, SlotVM b)
        {
            
            var idA = a?.Occupant?.Id;
            var idB = b?.Occupant?.Id;
            if (idA == null || idB == null) return;

            var min = idA < idB ? idA : idB;
            var max = idA < idB ? idB : idA;

            bool exists = Connections.Any(c =>
            {
                var ca = c.A?.Occupant?.Id;
                var cb = c.B?.Occupant?.Id;
                if (ca == null || cb == null) return false;
                var cmin = ca < cb ? ca : cb;
                var cmax = ca < cb ? cb : ca;
                return cmin == min && cmax == max;
            });

            if (!exists)
            {
                Connections.Add(new ConnectionVM(a, b));
            }
        }
    }

    public class SlotVM : BindableBase
    {
        public int Index { get; }
        private readonly DisplayViewModel _owner;

        private ReactorTemp _occupant;
        public ReactorTemp Occupant
        {
            get => _occupant;
            set => SetProperty(ref _occupant, value);
        }

        
        private System.Windows.FrameworkElement _element;
        public System.Windows.FrameworkElement Element
        {
            get => _element;
            set { if (_element != value) { _element = value; OnPropertyChanged(nameof(Element)); } }
        }

        public SlotVM(int index, DisplayViewModel owner)
        {
            Index = index;
            _owner = owner;
        }

        public void Place(ReactorTemp e) => _owner.PlaceInSlot(Index, e);
        public void Clear() => _owner.RemoveFromSlot(Index);
    }
}
