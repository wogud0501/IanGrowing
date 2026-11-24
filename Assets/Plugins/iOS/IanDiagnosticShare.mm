#import <UIKit/UIKit.h>
extern "C" UIViewController *UnityGetGLViewController(void);

extern "C" bool IanPresentReport(const char *path)
{
    UIViewController *host = UnityGetGLViewController();
    while (host.presentedViewController) host = host.presentedViewController;
    if (!host.view.window || !path) return false;
    NSURL *url = [NSURL fileURLWithPath:[NSString stringWithUTF8String:path]];
    UIActivityViewController *sheet = [[UIActivityViewController alloc] initWithActivityItems:@[url] applicationActivities:nil];
    sheet.popoverPresentationController.sourceView = host.view;
    sheet.popoverPresentationController.sourceRect = CGRectMake(CGRectGetMidX(host.view.bounds), CGRectGetMaxY(host.view.bounds)-1, 1, 1);
    [host presentViewController:sheet animated:YES completion:nil];
    return true;
}
