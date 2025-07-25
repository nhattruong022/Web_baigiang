﻿/*
 *   Plugin developed by CTRL+N.
 *
 *   LICENCE: GPL, LGPL, MPL
 *   NON-COMMERCIAL PLUGIN.
 *
 *   Website: https://www.ctrplusn.net/
 *   Facebook: https://www.facebook.com/ctrlplusn.net/
 *
 */
CKEDITOR.plugins.add('videoembed', {
    lang: 'fr,en',
    version: 1.1,
    init: function (editor) {
        // Command
        editor.addCommand('videoembed', new CKEDITOR.dialogCommand('videoembedDialog'));
        // Toolbar button
        editor.ui.addButton('VideoEmbed', {
            label: editor.lang.videoembed.button,
            command: 'videoembed',
            toolbar: 'Nhúng ',
            icon: '/ckeditor/plugins/videoembed/icons/videoembed.png'

        });
        // Dialog window
        CKEDITOR.dialog.add('videoembedDialog', '/ckeditor/plugins/videoembed/dialogs/videoembedDialog.js');
    }
});