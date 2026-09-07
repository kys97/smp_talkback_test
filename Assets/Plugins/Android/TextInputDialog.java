package com.namnyeochilse.accessibility;

import android.app.Activity;
import android.app.AlertDialog;
import android.text.InputFilter;
import android.text.InputType;
import android.view.WindowManager;
import android.widget.EditText;

/** Native editable text and standard dialog controls are exposed directly to TalkBack. */
public final class TextInputDialog {
    public interface Callback { void onResult(String value); }
    private final Activity activity;
    private AlertDialog dialog;
    private boolean closed;

    private TextInputDialog(Activity activity) { this.activity = activity; }

    public static TextInputDialog open(Activity activity, String label, String value, int limit, Callback callback) {
        TextInputDialog owner = new TextInputDialog(activity);
        activity.runOnUiThread(() -> {
            if (owner.closed || activity.isFinishing()) return;
            EditText editor = new EditText(activity);
            editor.setHint(label);
            editor.setSingleLine(true);
            editor.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_NO_SUGGESTIONS);
            if (limit > 0) editor.setFilters(new InputFilter[]{new InputFilter.LengthFilter(limit)});
            editor.setText(value);
            editor.setSelection(editor.length());
            int padding = (int)(24 * activity.getResources().getDisplayMetrics().density);
            editor.setPadding(padding, padding, padding, padding);
            owner.dialog = new AlertDialog.Builder(activity)
                .setTitle(label)
                .setView(editor)
                .setPositiveButton("입력 완료", (d, which) -> callback.onResult("1:" + editor.getText().toString()))
                .setNegativeButton("취소", (d, which) -> callback.onResult("0:"))
                .setOnCancelListener(d -> callback.onResult("0:"))
                .create();
            owner.dialog.getWindow().setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_VISIBLE);
            owner.dialog.show();
            editor.requestFocus();
        });
        return owner;
    }

    public void close() {
        activity.runOnUiThread(() -> {
            closed = true;
            if (dialog != null) dialog.dismiss();
        });
    }
}
