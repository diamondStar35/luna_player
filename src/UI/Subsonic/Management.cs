using LunaPlayer.Subsonic.Servers;
using WxSharp;

namespace LunaPlayer.UI.Subsonic;

internal sealed partial class BrowserDialog
{
    private void Manage()
    {
        var form = new Form(_window, Words.Manage, closeOnly: true);
        var list = Form.Field(form.Panel, form.Body, Words.Items,
            () => new ListCtrl(form.Panel, style: ListCtrlStyle.Report | ListCtrlStyle.SingleSelection), proportion: 1);
        list.MinSize = new Size(500, 260);
        list.InsertColumn(0, Words.Name, 160); list.InsertColumn(1, Words.Address, 250); list.InsertColumn(2, Words.Username, 120);
        Profile[] profiles = [];
        Button? edit = null, remove = null;
        void Refresh()
        {
            if (form.Token.IsCancellationRequested) return;
            profiles = _connections.Profiles; list.Clear();
            foreach (var profile in profiles) { var index = list.AddItem(profile.Name); list.SetItem(index, 1, profile.Server.AbsoluteUri); list.SetItem(index, 2, profile.Credentials.Username); }
        }
        void Edit(bool create)
        {
            var selected = list.SelectedIndex;
            if (!create && (selected < 0 || selected >= profiles.Length)) return;
            var editor = new ProfileEditor(form.Window, create ? null : profiles[(int)selected],
                async (value, token) => { await _connections.SaveAsync(value, token).ConfigureAwait(false); });
            _editors.Add(editor);
            editor.Closed += accepted =>
            {
                _editors.Remove(editor); editor.Dispose(); Refresh(); if (!form.Token.IsCancellationRequested) Buttons(); Profiles();
                if (accepted) _speak(Words.Saved);
            };
            editor.Show();
        }
        form.Button(Words.New, () => Edit(true));
        edit = form.Button(Words.Edit, () => Edit(false));
        remove = form.Button(Words.Remove, () =>
        {
            var selected = list.SelectedIndex;
            if (selected < 0 || selected >= profiles.Length || !form.Confirm(Words.ConfirmRemove)) return;
            var id = profiles[(int)selected].Id;
            Run(async token => { await _connections.RemoveAsync(id, token); return true; }, _ =>
            { _player.RemoveServerMedia(id); Refresh(); if (!form.Token.IsCancellationRequested) Buttons(); Profiles(); _speak(Words.Saved); }, form.Token, form.Error);
        });
        void Buttons() { edit?.Show(list.SelectedIndex >= 0); remove?.Show(list.SelectedIndex >= 0); form.Window.Layout(); }
        list.ItemSelected += (_, _) => Buttons(); list.ItemDeselected += (_, _) => Buttons(); list.ItemActivated += (_, _) => Edit(false);
        Refresh(); Buttons(); Keep(form, form); form.Show();
    }
}
