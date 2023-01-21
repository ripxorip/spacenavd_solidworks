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

		while (true) {
			CComPtr<IModelDoc2> iModelDoc2;
			sw->IGetFirstDocument2(&iModelDoc2);
			if (iModelDoc2 != NULL) {
				LPDISPATCH aw_ptr;
				HRESULT res = iModelDoc2->GetFirstModelView(&aw_ptr);
				IModelView* m_view = CComQIPtr<IModelView, &__uuidof(IModelView)>(aw_ptr);
				if (m_view != NULL) {
					m_view->TranslateBy(0.002, 0.00);
					m_view->RotateAboutCenter(10, 10);
				}
			}
			Sleep(500);
		}
	}

};

OBJECT_ENTRY_AUTO(__uuidof(spacenavd), Cspacenavd)
