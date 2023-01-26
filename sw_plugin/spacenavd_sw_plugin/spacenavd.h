// spacenavd.h : Declaration of the Cspacenavd

#pragma once
#include "resource.h"       // main symbols
#include <thread>

#include "spacenavdswplugin_i.h"

#import "sldworks.tlb" raw_interfaces_only, raw_native_types, no_namespace, named_guids
#import "swpublished.tlb" raw_interfaces_only, raw_native_types, no_namespace, named_guids 
#import "swconst.tlb" raw_interfaces_only, raw_native_types, no_namespace, named_guids 

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

#pragma comment(lib,"ws2_32.lib") //Winsock Library

using namespace ATL;
using namespace std;


// Cspacenavd

class ATL_NO_VTABLE Cspacenavd :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<Cspacenavd, &CLSID_spacenavd>,
	public IDispatchImpl<Ispacenavd, &IID_Ispacenavd, &LIBID_spacenavdswpluginLib, /*wMajor =*/ 1, /*wMinor =*/ 0>,
	public IDispatchImpl<ISwAddin, &__uuidof(ISwAddin), &LIBID_SWPublished, /* wMajor = */ 1, /* wMinor = */ 0>
{
public:
	Cspacenavd()
	{
	}

DECLARE_REGISTRY_RESOURCEID(106)


BEGIN_COM_MAP(Cspacenavd)
	COM_INTERFACE_ENTRY(Ispacenavd)
	COM_INTERFACE_ENTRY(IDispatch)
	COM_INTERFACE_ENTRY(ISwAddin)
END_COM_MAP()



	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

public:

private:
	CComPtr<ISldWorks> m_iSldWorks;
	thread t;
	IStream* pIStream;


// ISwAddin Methods
public:
	STDMETHOD(ConnectToSW)(LPDISPATCH ThisSW, long Cookie, VARIANT_BOOL * IsConnected)
	{
		/* Marshall to the background thread */
		/* Cred: https://www.codeproject.com/Articles/9506/Understanding-The-COM-Single-Threaded-Apartment-2 */
		IUnknown* pIUnknown = NULL;
		pIStream = NULL;
		ThisSW->QueryInterface(IID_IUnknown, (void**)&pIUnknown);
		if (pIUnknown) {
			::CoMarshalInterThreadInterfaceInStream(__uuidof(ISldWorks), pIUnknown, &pIStream);
			pIUnknown->Release();
			pIUnknown = NULL;
		}
		if (pIStream) {
			t = thread(&Cspacenavd::run_thread, this);
		}
		return S_OK;
	}

	STDMETHOD(DisconnectFromSW)(VARIANT_BOOL * IsDisconnected)
	{
		 return S_OK;
	}

	void run_thread() {
		/* Mashall to this thread */
		/* Cred: https://www.codeproject.com/Articles/9506/Understanding-The-COM-Single-Threaded-Apartment-2 */
		CComPtr<ISldWorks> sw = NULL;
		::CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
		::CoGetInterfaceAndReleaseStream(pIStream, __uuidof(ISldWorks), (void**)&sw);

		/* Socket */
#pragma warning(disable:4996) 
		WSADATA wsa;
		SOCKET s;
		struct sockaddr_in server;
		WSAStartup(MAKEWORD(2, 2), &wsa);
		s = socket(AF_INET, SOCK_STREAM, 0);
		server.sin_addr.s_addr = inet_addr("192.168.1.52");
		server.sin_family = AF_INET;
		server.sin_port = htons(11111);
		connect(s, (struct sockaddr*)&server, sizeof(server));
		uint8_t buf[256];
		int32_t *mouse_data;

		CComPtr<IModelDoc2> iModelDoc2;
		sw->IGetFirstDocument2(&iModelDoc2);
		LPDISPATCH aw_ptr;
		HRESULT res = iModelDoc2->GetFirstModelView(&aw_ptr);
		IModelView* m_view = CComQIPtr<IModelView, &__uuidof(IModelView)>(aw_ptr);

		while (true) {
			int ret = recv(s, (char*)buf, 256, 0); // Shall be 32
			mouse_data = (int32_t*)buf;

			double x = mouse_data[1];
			double y = mouse_data[2];
			double z = mouse_data[3];
			double rx = mouse_data[4];
			double ry = mouse_data[5];
			double rz = mouse_data[6];

			double move_coef = 0.00004;
			double rot_coef = 0.00030;
			double tilt_coef = 0.0014;
			double zoom_coef = 0.000000005;

			int thresh = 30;

			if (iModelDoc2 != NULL) {
				if (m_view != NULL) {
					/* Something like this?? */
					//m_view->put_EnableGraphicsUpdate(false);
					/* add some space in time in between? No, shall use translation3/orientation3!? */
					if (abs(x) > thresh || abs(y) > thresh)
					{
						m_view->TranslateBy(x * move_coef, y * move_coef);
						m_view->TranslateBy(x * move_coef, y * move_coef);
						m_view->TranslateBy(x * move_coef, y * move_coef);
					}
					if (abs(rx) > thresh || abs(ry) > thresh)
					{
						m_view->RotateAboutCenter(rx * rot_coef, ry * rot_coef);
						m_view->RotateAboutCenter(rx * rot_coef, ry * rot_coef);
						m_view->RotateAboutCenter(rx * rot_coef, ry * rot_coef);
					}
					if (abs(rz) > thresh)
					{
						m_view->RollBy(-rz * rot_coef);
						m_view->RollBy(-rz * rot_coef);
						m_view->RollBy(-rz * rot_coef);
					}
					//m_view->put_EnableGraphicsUpdate(true);
				}
			}
		}
	}

};

OBJECT_ENTRY_AUTO(__uuidof(spacenavd), Cspacenavd)
