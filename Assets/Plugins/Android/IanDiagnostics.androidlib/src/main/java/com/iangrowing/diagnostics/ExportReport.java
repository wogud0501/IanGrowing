package com.iangrowing.diagnostics;

import android.app.Activity;
import android.app.Fragment;
import android.content.ClipData;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.widget.Toast;
import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;

public final class ExportReport extends Fragment {
    // Text is the immutable preview confirmed in Unity. Never accept an arbitrary filesystem path.
    public static void open(Activity host, String name, String preview, boolean share) {
        host.runOnUiThread(() -> {
            try {
                if (share) {
                    File file = ReportProvider.report(host, name);
                    file.getParentFile().mkdirs();
                    try (FileOutputStream stream = new FileOutputStream(file)) {
                        stream.write(preview.getBytes(StandardCharsets.UTF_8));
                    }
                    Uri uri = new Uri.Builder().scheme("content").authority(host.getPackageName() + ".ianreports")
                            .appendPath(name).build();
                    Intent send = new Intent(Intent.ACTION_SEND).setType("text/plain");
                    send.putExtra(Intent.EXTRA_STREAM, uri);
                    send.setClipData(ClipData.newRawUri("report", uri));
                    send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                    host.startActivity(Intent.createChooser(send, null));
                } else if (host.getFragmentManager().findFragmentByTag("ian-export") == null) {
                    ExportReport fragment = new ExportReport();
                    Bundle args = new Bundle();
                    args.putString("name", name);
                    args.putString("preview", preview);
                    fragment.setArguments(args);
                    host.getFragmentManager().beginTransaction().add(fragment, "ian-export").commit();
                }
            } catch (Exception e) {
                Toast.makeText(host, "내보내기를 열 수 없습니다.", Toast.LENGTH_LONG).show();
            }
        });
    }

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        if (state != null) return;
        Intent create = new Intent(Intent.ACTION_CREATE_DOCUMENT).setType("text/plain");
        create.addCategory(Intent.CATEGORY_OPENABLE);
        create.putExtra(Intent.EXTRA_TITLE, getArguments().getString("name"));
        try {
            startActivityForResult(create, 1);
        } catch (RuntimeException e) {
            Toast.makeText(getActivity(), "파일 선택기를 열 수 없습니다.", Toast.LENGTH_LONG).show();
            dismiss();
        }
    }

    @Override public void onActivityResult(int request, int result, Intent data) {
        if (request != 1) return;
        if (result == Activity.RESULT_OK && data != null && data.getData() != null) {
            try (OutputStream out = getActivity().getContentResolver().openOutputStream(data.getData(), "wt")) {
                out.write(getArguments().getString("preview").getBytes(StandardCharsets.UTF_8));
                Toast.makeText(getActivity(), "파일을 저장했습니다.", Toast.LENGTH_SHORT).show();
            } catch (Exception e) {
                Toast.makeText(getActivity(), "파일을 저장할 수 없습니다.", Toast.LENGTH_LONG).show();
            }
        }
        dismiss();
    }
    private void dismiss() {
        getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
    }
}
