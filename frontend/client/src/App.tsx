import { Toaster } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import NotFound from "@/pages/NotFound";
import { Route, Switch } from "wouter";
import ErrorBoundary from "./components/ErrorBoundary";
import { ThemeProvider } from "./contexts/ThemeContext";
import LoginPage from "./pages/LoginPage";
import DashboardPage from "./pages/DashboardPage";
import PurchasePage from "./pages/PurchasePage";
import SalesPage from "./pages/SalesPage";
import InventoryPage from "./pages/InventoryPage";
import SupplierPage from "./pages/SupplierPage";
import CustomerPage from "./pages/CustomerPage";
import CustomerDetailPage from "./pages/CustomerDetailPage";
import CustomerRecycleBinPage from "./pages/CustomerRecycleBinPage";
import CustomerBlacklistPage from "./pages/CustomerBlacklistPage";
import ReportsPage from "./pages/ReportsPage";
import RequirementPage from "./pages/RequirementPage";
import QuotationPage from "./pages/QuotationPage";
import BomPage from "./pages/BomPage";
import Home from "./pages/Home";
import MaterialDetailPage from "./pages/MaterialDetailPage";
import ProfilePage from "./pages/ProfilePage";

function Router() {
  return (
    <Switch>
      <Route path={"/"} component={Home} />
      <Route path={"/login"} component={LoginPage} />
      <Route path={"/dashboard"} component={DashboardPage} />
      <Route path={"/purchase"} component={PurchasePage} />
      <Route path={"/sales"} component={SalesPage} />
      <Route path={"/inventory"} component={InventoryPage} />
      <Route path={"/supplier"} component={SupplierPage} />
      <Route path={"/customer"} component={CustomerPage} />
      <Route path={"/customer/:id"} component={CustomerDetailPage} />
      <Route path={"/customer-recycle-bin"} component={CustomerRecycleBinPage} />
      <Route path={"/customer-blacklist"} component={CustomerBlacklistPage} />
      <Route path={"/reports"} component={ReportsPage} />
      <Route path={"/requirement"} component={RequirementPage} />
      <Route path={"/quotation"} component={QuotationPage} />
      <Route path={"/bom"} component={BomPage} />
      <Route path={"/material/:partNo"} component={MaterialDetailPage} />
      <Route path={"/material"} component={MaterialDetailPage} />
      <Route path={"/profile"} component={ProfilePage} />
      <Route path={"/404"} component={NotFound} />
      <Route component={NotFound} />
    </Switch>
  );
}

function App() {
  return (
    <ErrorBoundary>
      <ThemeProvider defaultTheme="dark">
        <TooltipProvider>
          <Toaster />
          <Router />
        </TooltipProvider>
      </ThemeProvider>
    </ErrorBoundary>
  );
}

export default App;
